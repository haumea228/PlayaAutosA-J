using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class CitaViewModel
    {
        public int CitaId { get; set; }

        [Required(ErrorMessage = "Seleccione un cliente.")]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Display(Name = "Vehículo de interés")]
        public int? VehiculoId { get; set; }

        [Required(ErrorMessage = "Seleccione un vendedor.")]
        [Display(Name = "Vendedor asignado")]
        public int VendedorId { get; set; }

        [Display(Name = "Estado")]
        public int EstadoCitaId { get; set; } = 1;

        [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
        [Display(Name = "Fecha y hora")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; } = DateTime.Today.AddHours(10);

        [Required(ErrorMessage = "Seleccione el tipo de cita.")]
        [Display(Name = "Tipo de cita")]
        [StringLength(20)]
        public string TipoCita { get; set; } = "Visita";

        [Display(Name = "Observaciones")]
        [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
        public string? Observaciones { get; set; }

        // ── Dropdowns ────────────────────────────────────────────────
        public List<SelectListItem> Clientes { get; set; } = new();
        public List<SelectListItem> Vehiculos { get; set; } = new();
        public List<SelectListItem> Vendedores { get; set; } = new();
        public List<SelectListItem> EstadosCita { get; set; } = new();
        public List<SelectListItem> TiposCita { get; set; } = new()
        {
            new SelectListItem("Visita", "Visita"),
            new SelectListItem("Test Drive", "TestDrive"),
            new SelectListItem("Consulta", "Consulta")
        };

        // Para mostrar en la vista
        public string? NombreCliente { get; set; }
        public string? VehiculoInfo { get; set; }
        public string? VendedorNombre { get; set; }
        public string? EstadoDescripcion { get; set; }
        public string? EstadoColor { get; set; }
    }

    public class CitaListItem
    {
        public int CitaId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string? Vehiculo { get; set; }
        public string Vendedor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? EstadoColor { get; set; }
        public int EstadoCitaId { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoCita { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}