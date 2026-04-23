using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CoordinadorController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(cursos);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Curso curso)
        {
            if (curso.Creditos <= 0)
                ModelState.AddModelError("Creditos", "Los créditos deben ser mayores a 0.");

            if (curso.HorarioFin <= curso.HorarioInicio)
                ModelState.AddModelError("HorarioFin", "La hora fin debe ser mayor a la hora inicio.");

            if (!ModelState.IsValid)
                return View(curso);

            curso.Activo = true;

            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            _cache.Remove("cursos_activos");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Editar(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);

            if (curso == null)
                return NotFound();

            return View(curso);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Curso curso)
        {
            if (id != curso.Id)
                return NotFound();

            if (curso.Creditos <= 0)
                ModelState.AddModelError("Creditos", "Los créditos deben ser mayores a 0.");

            if (curso.HorarioFin <= curso.HorarioInicio)
                ModelState.AddModelError("HorarioFin", "La hora fin debe ser mayor a la hora inicio.");

            if (!ModelState.IsValid)
                return View(curso);

            _context.Update(curso);
            await _context.SaveChangesAsync();

            _cache.Remove("cursos_activos");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Desactivar(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);

            if (curso == null)
                return NotFound();

            curso.Activo = false;
            await _context.SaveChangesAsync();

            _cache.Remove("cursos_activos");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Matriculas(int cursoId)
        {
            var curso = await _context.Cursos
                .Include(c => c.Matriculas)
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null)
                return NotFound();

            return View(curso);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarMatricula(int id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);

            if (matricula == null)
                return NotFound();

            matricula.Estado = EstadoMatricula.Confirmada;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Matriculas), new { cursoId = matricula.CursoId });
        }

        [HttpPost]
        public async Task<IActionResult> CancelarMatricula(int id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);

            if (matricula == null)
                return NotFound();

            matricula.Estado = EstadoMatricula.Cancelada;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Matriculas), new { cursoId = matricula.CursoId });
        }
    }
}