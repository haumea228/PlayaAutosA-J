using Microsoft.AspNetCore.Mvc.Rendering;

namespace PlayaAutos.Web.Models
{
    public class GastoViewModel
    {
        public int GastoId { get; set; }
        public int VehiculoId { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public string TipoGasto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Proveedor { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class CrearGastoViewModel
    {
        public int VehiculoId { get; set; }
        public int TipoGastoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Proveedor { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;

        public List<SelectListItem> Vehiculos { get; set; } = new();
        public List<SelectListItem> TiposGasto { get; set; } = new();
    }

    public class GastoListItem
    {
        public int GastoId { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public string TipoGasto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Proveedor { get; set; }
        public DateTime Fecha { get; set; }
    }
}
