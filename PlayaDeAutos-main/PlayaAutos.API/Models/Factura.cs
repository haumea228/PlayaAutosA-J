namespace PlayaAutos.API.Models
{
    public class Factura
    {
        public int FacturaId { get; set; }
        public int VentaId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public long Subtotal { get; set; }
        public long IVA { get; set; }
        public long Total { get; set; }
        public string? RutaPDF { get; set; }
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public int UsuarioGeneracion { get; set; }
        public int TimbradoId { get; set; }

        public Venta Venta { get; set; } = null!;
        public Timbrado Timbrado { get; set; } = null!;
    }
}