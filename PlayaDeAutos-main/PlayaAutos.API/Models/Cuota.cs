namespace PlayaAutos.API.Models
{
    public class Cuota
    {
        public int CuotaId { get; set; }
        public int VentaId { get; set; }
        public int NumeroCuota { get; set; }
        public long Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public long? MontoPagado { get; set; }
        public long? MontoRecargo { get; set; }
        public int? FormaPagoId { get; set; }
        public string? ComprobanteImagen { get; set; }
        public string? ObservacionPago { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public DateTime? FechaRegistroPago { get; set; }

        public Venta Venta { get; set; } = null!;
        public FormaPago? FormaPago { get; set; }
        public MovimientoCaja? MovimientoCaja { get; set; }
        public ICollection<SolicitudPagoCuota> SolicitudesPago { get; set; } = new List<SolicitudPagoCuota>();
        public ICollection<PagoOnlineCuota> PagosOnline { get; set; } = new List<PagoOnlineCuota>();
    }
}