namespace PlayaAutos.API.DTOs
{
    public class FacturaDto
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string Timbrado { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;
        public long Subtotal { get; set; }
        public long IVA { get; set; }
        public long Total { get; set; }
        public string? RutaPDF { get; set; }
    }

    public class CrearFacturaDto
    {
        public int VentaId { get; set; }
        public int TimbradoId { get; set; }
        public DateTime FechaEmision { get; set; }
        public long Subtotal { get; set; }
        public long IVA { get; set; }
        public long Total { get; set; }
    }
}