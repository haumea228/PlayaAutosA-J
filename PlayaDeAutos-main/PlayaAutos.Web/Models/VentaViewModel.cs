using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class PagoVentaViewModel
    {
        public int FormaPagoId { get; set; }
        public long Monto { get; set; }
        public string? Observacion { get; set; }
        public int? TasacionVehiculoId { get; set; }
        public string? ComprobanteImagen { get; set; }
    }

    public class TasacionSelectItem
    {
        public int TasacionVehiculoId { get; set; }
        public int ClienteId { get; set; }
        public string Label { get; set; } = string.Empty;
        public long PrecioVenta { get; set; }
        public long ValorTasacion { get; set; }
    }

    public class VentaViewModel
    {
        [Required(ErrorMessage = "Seleccione un cliente.")]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "Seleccione un vehículo.")]
        [Display(Name = "Vehículo")]
        public int VehiculoId { get; set; }

        [Required(ErrorMessage = "Seleccione un tipo de venta.")]
        [Display(Name = "Tipo de venta")]
        public int TipoVentaId { get; set; }

        public int VendedorId { get; set; }

        [Required(ErrorMessage = "La fecha de venta es obligatoria.")]
        [Display(Name = "Fecha de venta")]
        [DataType(DataType.Date)]
        public DateTime FechaVenta { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "El monto total es obligatorio.")]
        [Range(1, long.MaxValue, ErrorMessage = "El monto total debe ser mayor a cero.")]
        [Display(Name = "Monto total (Gs.)")]
        public long MontoTotal { get; set; }

        [Display(Name = "% Recargo por atraso")]
        [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100.")]
        public decimal? PorcentajeRecargo { get; set; }

        [Display(Name = "Monto de entrada (Gs.)")]
        [Range(0, long.MaxValue)]
        public long? MontoEntrada { get; set; }

        [Display(Name = "Saldo financiado (Gs.)")]
        [Range(0, long.MaxValue)]
        public long? SaldoFinanciado { get; set; }

        [Display(Name = "Tasa de interés (%)")]
        [Range(0, 100)]
        public decimal? TasaInteres { get; set; }

        [Display(Name = "Cantidad de cuotas")]
        [Range(1, 120)]
        public int? CantidadCuotas { get; set; }

        [ValidateNever]
        public List<PagoVentaViewModel> Pagos { get; set; } = new();

        [ValidateNever] public List<SelectListItem> Clientes { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Vehiculos { get; set; } = new();
        [ValidateNever] public List<SelectListItem> TiposVenta { get; set; } = new();
        [ValidateNever] public List<SelectListItem> FormasPago { get; set; } = new();
        [ValidateNever] public List<TasacionSelectItem> TasacionesAprobadas { get; set; } = new();

        // 🟢 NUEVO
        [ValidateNever] public List<TipoVentaListItem> TiposVentaData { get; set; } = new();
    }

    public class VentaListItem
    {
        public int VentaId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public string TipoVenta { get; set; } = string.Empty;
        public string FormasPago { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public long MontoTotal { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    // 🟢 NUEVO
    public class TipoVentaListItem
    {
        public int TipoVentaId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool RequiereFinanciacion { get; set; }
    }
}