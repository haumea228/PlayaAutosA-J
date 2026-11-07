namespace PlayaAutos.API.Models
{
    public class PagoVenta
    {
        public int PagoVentaId { get; set; }
        public int VentaId { get; set; }
        public int FormaPagoId { get; set; }
        public long Monto { get; set; }
        public string? ComprobanteImagen { get; set; }
        public string? Observacion { get; set; }
        public int? TasacionVehiculoId { get; set; } // solo cuando FormaPago es Permuta

        public Venta Venta { get; set; } = null!;
        public FormaPago FormaPago { get; set; } = null!;
        public TasacionVehiculo? TasacionVehiculo { get; set; }
    }
}