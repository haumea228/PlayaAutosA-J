namespace PlayaAutos.API.Models
{
    public class NotaCredito
    {
        public int NotaCreditoId { get; set; }
        public int VentaId { get; set; }
        public string NumeroNota { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public long Monto { get; set; }
        public int VehiculoId { get; set; }
        public string? RutaPDF { get; set; }
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public int UsuarioGeneracion { get; set; }
        public int TimbradoId { get; set; }
        public int? TasacionVehiculoId { get; set; }

        public Venta Venta { get; set; } = null!;
        public Vehiculo Vehiculo { get; set; } = null!;
        public Timbrado Timbrado { get; set; } = null!;
        public TasacionVehiculo? TasacionVehiculo { get; set; }
    }
}