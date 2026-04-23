using PortalAcademico.Models;

namespace PortalAcademico.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Si ya hay cursos, no hace nada
            if (context.Cursos.Any()) return;

            context.Cursos.AddRange(
                new Curso
                {
                    Codigo = "CURS001",
                    Nombre = "Matemática I",
                    Creditos = 4,
                    CupoMaximo = 30,
                    HorarioInicio = DateTime.Now.AddHours(1),
                    HorarioFin = DateTime.Now.AddHours(3),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "CURS002",
                    Nombre = "Programación I",
                    Creditos = 5,
                    CupoMaximo = 25,
                    HorarioInicio = DateTime.Now.AddHours(4),
                    HorarioFin = DateTime.Now.AddHours(6),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "CURS003",
                    Nombre = "Base de Datos",
                    Creditos = 4,
                    CupoMaximo = 20,
                    HorarioInicio = DateTime.Now.AddHours(7),
                    HorarioFin = DateTime.Now.AddHours(9),
                    Activo = true
                }
            );

            context.SaveChanges();
        }
    }
}