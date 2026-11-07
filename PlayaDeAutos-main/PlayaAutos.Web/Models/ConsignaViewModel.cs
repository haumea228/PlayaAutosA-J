using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class ConsignaViewModel
    {
        [Required(ErrorMessage = "Seleccione un consignante.")]
        [Display(Name = "Consignante")]
        public int ConsignanteId { get; set; }

        [Required(ErrorMessage = "Seleccione una tasación.")]
        [Display(Name = "Tasación del vehículo")]
        public int TasacionVehiculoId { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de fin")]
        public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(90);

        [Required(ErrorMessage = "Ingrese el porcentaje de comisión.")]
        [Range(0.01, 100, ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
        [Display(Name = "Comisión (%)")]
        public decimal PorcentajeComision { get; set; }

        [Display(Name = "Cláusulas adicionales")]
        public string? Clausulas { get; set; }

        [ValidateNever] public List<SelectListItem> Consignantes { get; set; } = new();
    }

    public class ConsignaListItem
    {
        public int ContratoId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public string Consignante { get; set; } = string.Empty;
        public string ConsignanteCI { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal PorcentajeComision { get; set; }
        public long PrecioCliente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
    }

    public class ConsignaContratoViewModel
    {
        public int ContratoId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DuracionDias { get; set; }
        public decimal PorcentajeComision { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Clausulas { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public string ConsignanteNombre { get; set; } = string.Empty;
        public string? ConsignanteCI { get; set; }
        public string VehiculoMarca { get; set; } = string.Empty;
        public string VehiculoModelo { get; set; } = string.Empty;
        public int VehiculoAnio { get; set; }
        public string? VehiculoColor { get; set; }
        public long? VehiculoKilometraje { get; set; }
        public string? VehiculoTipo { get; set; }
        public long PrecioVentaCliente { get; set; }
        public long PrecioVentaPlaya { get; set; }
    }
}
