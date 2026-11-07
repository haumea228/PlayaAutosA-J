using Microsoft.AspNetCore.Mvc.Rendering;

namespace PlayaAutos.Web.Models
{
    public class MovimientoCajaViewModel
    {
        public int MovimientoId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Signo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Referencia { get; set; }
        public string? Comentarios { get; set; }
    }

    public class CrearMovimientoViewModel
    {
        public DateTime Fecha { get; set; } = DateTime.Today;
        public int TipoMovimientoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Comentarios { get; set; }
        public List<SelectListItem> TiposMovimiento { get; set; } = new();

        // Campos para egreso por gasto de vehículo
        public int? VehiculoId { get; set; }
        public int? TipoGastoId { get; set; }
        public string? Proveedor { get; set; }
        public List<SelectListItem> Vehiculos { get; set; } = new();
        public List<SelectListItem> TiposGasto { get; set; } = new();
        // ID del tipo "Egreso por Gasto" para que la vista pueda detectarlo
        public int TipoEgresoGastoId { get; set; }
    }

    public class BalanceViewModel
    {
        public long TotalIngresos { get; set; }
        public long TotalEgresos { get; set; }
        public long Saldo { get; set; }
        public int CantidadMovimientos { get; set; }
    }

    public class CajaListItem
    {
        public int MovimientoId { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Signo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Referencia { get; set; }
        public string? Comentarios { get; set; }
    }

    public class CierreCajaViewModel
    {
        public int CierreId { get; set; }
        public DateTime FechaCierre { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public long TotalIngresos { get; set; }
        public long TotalEgresos { get; set; }
        public long SaldoNeto { get; set; }
        public int CantidadMovimientos { get; set; }
        public string? Observacion { get; set; }
    }
}