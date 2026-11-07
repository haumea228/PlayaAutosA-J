using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class TimbradoViewModel
    {
        [Required(ErrorMessage = "El número de timbrado es obligatorio.")]
        [StringLength(20, ErrorMessage = "El número no puede superar los 20 caracteres.")]
        [Display(Name = "Número de timbrado")]
        public string NumeroTimbrado { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de vencimiento")]
        public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddYears(1);

        [Required(ErrorMessage = "El número desde es obligatorio.")]
        [Range(1, long.MaxValue, ErrorMessage = "El número desde debe ser mayor a cero.")]
        [Display(Name = "Número desde")]
        public long NumeroDesde { get; set; } = 1;

        [Required(ErrorMessage = "El número hasta es obligatorio.")]
        [Range(1, long.MaxValue, ErrorMessage = "El número hasta debe ser mayor a cero.")]
        [Display(Name = "Número hasta")]
        public long NumeroHasta { get; set; }
    }

    public class TimbradoListItem
    {
        public int TimbradoId { get; set; }
        public string NumeroTimbrado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public long NumeroDesde { get; set; }
        public long NumeroHasta { get; set; }
        public long UltimoNumeroUsado { get; set; }
        public bool Activo { get; set; }
    }
}
