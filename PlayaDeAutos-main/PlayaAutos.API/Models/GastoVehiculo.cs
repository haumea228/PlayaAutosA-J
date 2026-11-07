namespace PlayaAutos.API.Models
{
    public class GastoVehiculo
    {
        public int GastoId { get; set; }
        public int VehiculoId { get; set; }
        public int TipoGastoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Proveedor { get; set; }
        public DateTime Fecha { get; set; }
        public int UsuarioRegistro { get; set; }

        public Vehiculo Vehiculo { get; set; } = null!;
        public TipoGasto TipoGasto { get; set; } = null!;
        public MovimientoCaja? MovimientoCaja { get; set; }
    }
}