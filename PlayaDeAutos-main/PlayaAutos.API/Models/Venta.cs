namespace PlayaAutos.API.Models
{
    public class Venta
    {
        public int VentaId { get; set; }
        public int ClienteId { get; set; }
        public int VehiculoId { get; set; }
        public int VendedorId { get; set; }
        public int TipoVentaId { get; set; }
        public DateTime FechaVenta { get; set; }
        public long MontoTotal { get; set; }
        public long? MontoEntrada { get; set; }
        public long? SaldoFinanciado { get; set; }
        public decimal? TasaInteres { get; set; }
        public int? CantidadCuotas { get; set; }
        public string Estado { get; set; } = "Registrada";
        public DateTime? FechaAnulacion { get; set; }
        public string? MotivoAnulacion { get; set; }
        public decimal? PorcentajeRecargo { get; set; }

        public Cliente Cliente { get; set; } = null!;
        public Vehiculo Vehiculo { get; set; } = null!;
        public Usuario Vendedor { get; set; } = null!;
        public TipoVenta TipoVenta { get; set; } = null!;

        public ICollection<PagoVenta> PagosVenta { get; set; } = new List<PagoVenta>();
        public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
        public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = new List<MovimientoCaja>();
        public Factura? Factura { get; set; }
        public NotaCredito? NotaCredito { get; set; }
    }
}