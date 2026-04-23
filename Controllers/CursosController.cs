using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

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
    }
}