namespace PlayaAutos.API.Models
{
    public class MovimientoCaja
    {
        public int MovimientoId { get; set; }
        public DateTime Fecha { get; set; }
        public int TipoMovimientoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public int? VentaId { get; set; }
        public int? CuotaId { get; set; }
        public int? GastoId { get; set; }
        public int? FormaPagoId { get; set; }
        public int UsuarioRegistro { get; set; }
        public string? Comentarios { get; set; }

        public TipoMovimiento TipoMovimiento { get; set; } = null!;
        public Venta? Venta { get; set; }
        public Cuota? Cuota { get; set; }
        public GastoVehiculo? GastoVehiculo { get; set; }
        
        // Cierre Caja
        public int? CierreId { get; set; }
        public CierreCaja? Cierre { get; set; }
    }
}