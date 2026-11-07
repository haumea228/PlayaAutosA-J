namespace PlayaAutos.Web.Models
{
    public class NotaCreditoViewModel
    {
        public int NotaCreditoId { get; set; }
        public int VentaId { get; set; }
        public string NumeroNota { get; set; } = string.Empty;
        public string Timbrado { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string ClienteRUC { get; set; } = string.Empty;
        public string? ClienteDireccion { get; set; }
        public string? ClienteTelefono { get; set; }
        public string VehiculoVenta { get; set; } = string.Empty;
        public string? VehiculoPermuta { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public long Monto { get; set; }
        public int? TasacionVehiculoId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
    }

    public class NotaCreditoListItem
    {
        public int NotaCreditoId { get; set; }
        public int VentaId { get; set; }
        public string NumeroNota { get; set; } = string.Empty;
        public string Timbrado { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string VehiculoVenta { get; set; } = string.Empty;
        public string? VehiculoPermuta { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public long Monto { get; set; }
    }
}