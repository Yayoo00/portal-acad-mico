using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PortalAcademico.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
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

            IQueryable<Curso> query = _context.Cursos.Where(c => c.Activo);

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                query = query.Where(c => c.Nombre.Contains(nombre));
            }

            if (minCreditos.HasValue)
            {
                query = query.Where(c => c.Creditos >= minCreditos.Value);
            }

            if (maxCreditos.HasValue)
            {
                query = query.Where(c => c.Creditos <= maxCreditos.Value);
            }

            if (horario.HasValue)
            {
                var hora = horario.Value.TimeOfDay;
                query = query.Where(c => c.HorarioInicio.TimeOfDay <= hora && c.HorarioFin.TimeOfDay >= hora);
            }

            var cursos = await query.OrderBy(c => c.Nombre).ToListAsync();

            ViewBag.Nombre = nombre;
            ViewBag.MinCreditos = minCreditos;
            ViewBag.MaxCreditos = maxCreditos;
            ViewBag.Horario = horario?.ToString("HH:mm");

            return View(cursos);
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