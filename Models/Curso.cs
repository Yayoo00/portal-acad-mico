using System.ComponentModel.DataAnnotations;

namespace PortalAcademico.Models
{
    public class Curso
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Creditos debe ser mayor a 0")]
        public int Creditos { get; set; }

        public int CupoMaximo { get; set; }

        public DateTime HorarioInicio { get; set; }
        public DateTime HorarioFin { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Matricula> Matriculas { get; set; }
    }
}