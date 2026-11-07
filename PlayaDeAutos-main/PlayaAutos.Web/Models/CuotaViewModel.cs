using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PlayaAutos.Web.Models
{
    public class CuotaListItem
    {
        public int CuotaId { get; set; }
        public int VentaId { get; set; }
        public int NumeroCuota { get; set; }
        public long Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public long? MontoPagado { get; set; }
        public long? MontoRecargo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;
        public int? CantidadCuotas { get; set; }
        public string? ComprobanteImagen { get; set; }
        /// <summary>Pago informado desde la app (pendiente o aprobado), si lo hay.</summary>
        public int? SolicitudPagoId { get; set; }
    }

    /// <summary>Pago de cuota informado por el cliente desde la app.</summary>
    public class SolicitudPagoCuotaItem
    {
        public int SolicitudPagoCuotaId { get; set; }
        public int CuotaId { get; set; }
        public int VentaId { get; set; }
        public int NumeroCuota { get; set; }
        public int? CantidadCuotas { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string? ClienteCI { get; set; }
        public string? ClienteTelefono { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public DateTime FechaVencimiento { get; set; }
        public DateTime FechaReporte { get; set; }
        public long Monto { get; set; }
        public long MontoRecargo { get; set; }
        public long Total { get; set; }
        public string? ObservacionCliente { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class PendientesViewModel
    {
        public List<VentaListItem> Ventas { get; set; } = new();
        public List<SolicitudPagoCuotaItem> PagosCuotas { get; set; } = new();
        public List<SelectListItem> FormasPago { get; set; } = new();
        /// <summary>"ventas" | "cuotas": pestaña activa.</summary>
        public string Tab { get; set; } = "ventas";
    }

    public class PagarCuotaViewModel
    {
        public int CuotaId { get; set; }
        public int NumeroCuota { get; set; }
        public long Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;
        public int? CantidadCuotas { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.Today;
        public long MontoPagado { get; set; }
        public long? MontoRecargo { get; set; }
        public int FormaPagoId { get; set; }
        public string? ObservacionPago { get; set; }
        public IFormFile? Comprobante { get; set; }public decimal? RecargoAutomatico { get; set; }

        public List<SelectListItem> FormasPago { get; set; } = new();
    }
}
