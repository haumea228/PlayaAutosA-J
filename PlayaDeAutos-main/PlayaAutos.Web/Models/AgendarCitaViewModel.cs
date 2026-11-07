using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class AgendarCitaViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        public string CI_RUC { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        public string Direccion { get; set; } = string.Empty;

        public int? VehiculoId { get; set; }
        public string? VehiculoInfo { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
        public DateTime FechaHora { get; set; }

        [Required(ErrorMessage = "El tipo de cita es obligatorio.")]
        public string TipoCita { get; set; } = "Consulta";

        public string? Observaciones { get; set; }
    }
}
