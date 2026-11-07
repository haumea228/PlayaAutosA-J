using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class ClienteViewModel
    {
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El CI/RUC es obligatorio.")]
        [StringLength(20, ErrorMessage = "El CI/RUC no puede superar los 20 caracteres.")]
        [Display(Name = "CI / RUC")]
        public string CI_RUC { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
        [StringLength(100, ErrorMessage = "El email no puede superar los 100 caracteres.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [Display(Name = "Precio mínimo de interés")]
        [Range(0, long.MaxValue, ErrorMessage = "El precio mínimo debe ser un valor positivo.")]
        public long? RangoPrecioMin { get; set; }

        [Display(Name = "Precio máximo de interés")]
        [Range(0, long.MaxValue, ErrorMessage = "El precio máximo debe ser un valor positivo.")]
        public long? RangoPrecioMax { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public bool EsConsignante { get; set; }
        public int? ConsignanteId { get; set; }
        public decimal? PorcentajeComisionDefault { get; set; }
    }

    public class ClienteListItem
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string CI_RUC { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
    }
}