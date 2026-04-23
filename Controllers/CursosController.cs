using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace PortalAcademico.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CursosController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string? nombre, int? minCreditos, int? maxCreditos, DateTime? horario)
        {
            if (minCreditos.HasValue && minCreditos.Value < 0)
    {
        ModelState.AddModelError("minCreditos", "No se aceptan créditos negativos.");
    }

    if (maxCreditos.HasValue && maxCreditos.Value < 0)
    {
        ModelState.AddModelError("maxCreditos", "No se aceptan créditos negativos.");
    }

    var cacheKey = "cursos_activos";

    if (!_cache.TryGetValue(cacheKey, out List<Curso>? cursosCacheados))
    {
        cursosCacheados = await _context.Cursos
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));

        _cache.Set(cacheKey, cursosCacheados, cacheOptions);
    }

    var cursos = cursosCacheados.AsQueryable();

    if (!string.IsNullOrWhiteSpace(nombre))
    {
        cursos = cursos.Where(c => c.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
    }

    if (minCreditos.HasValue)
    {
        cursos = cursos.Where(c => c.Creditos >= minCreditos.Value);
    }

    if (maxCreditos.HasValue)
    {
        cursos = cursos.Where(c => c.Creditos <= maxCreditos.Value);
    }

    if (horario.HasValue)
    {
        var hora = horario.Value.TimeOfDay;
        cursos = cursos.Where(c => c.HorarioInicio.TimeOfDay <= hora && c.HorarioFin.TimeOfDay >= hora);
    }

    ViewBag.Nombre = nombre;
    ViewBag.MinCreditos = minCreditos;
    ViewBag.MaxCreditos = maxCreditos;
    ViewBag.Horario = horario?.ToString("HH:mm");

    return View(cursos.ToList());
        }

        public async Task<IActionResult> Detalle(int id)
    {
        var curso = await _context.Cursos
        .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

    if (curso == null)
    {
        return NotFound();
    }

    if (curso.HorarioFin <= curso.HorarioInicio)
    {
        ModelState.AddModelError("", "El horario del curso es inválido.");
    }

    HttpContext.Session.SetInt32("UltimoCursoId", curso.Id);
    HttpContext.Session.SetString("UltimoCursoNombre", curso.Nombre);

    return View(curso);
}

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Inscribirse(int cursoId)
        {
            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.Id == cursoId && c.Activo);

            if (curso == null)
            {
                TempData["Error"] = "El curso no existe o no está activo.";
                return RedirectToAction("Index");
            }

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(usuarioId))
            {
                TempData["Error"] = "Debes iniciar sesión para inscribirte.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var yaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.CursoId == cursoId && m.UsuarioId == usuarioId && m.Estado != EstadoMatricula.Cancelada);

            if (yaMatriculado)
            {
                TempData["Error"] = "Ya estás matriculado en este curso.";
                return RedirectToAction("Detalle", new { id = cursoId });
            }

            var totalMatriculados = await _context.Matriculas
                .CountAsync(m => m.CursoId == cursoId && m.Estado != EstadoMatricula.Cancelada);

            if (totalMatriculados >= curso.CupoMaximo)
            {
                TempData["Error"] = "No hay cupos disponibles para este curso.";
                return RedirectToAction("Detalle", new { id = cursoId });
            }

            var matriculasUsuario = await _context.Matriculas
                .Where(m => m.UsuarioId == usuarioId && m.Estado != EstadoMatricula.Cancelada)
                .Include(m => m.Curso)
                .ToListAsync();

            bool hayCruceHorario = matriculasUsuario.Any(m =>
                curso.HorarioInicio < m.Curso.HorarioFin &&
                curso.HorarioFin > m.Curso.HorarioInicio);

            if (hayCruceHorario)
            {
                TempData["Error"] = "El horario del curso se cruza con otra matrícula registrada.";
                return RedirectToAction("Detalle", new { id = cursoId });
            }

            var matricula = new Matricula
            {
                CursoId = cursoId,
                UsuarioId = usuarioId,
                FechaRegistro = DateTime.Now,
                Estado = EstadoMatricula.Pendiente
            };

            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Inscripción realizada correctamente. Estado: Pendiente.";
            return RedirectToAction("Detalle", new { id = cursoId });
        }
    }
}