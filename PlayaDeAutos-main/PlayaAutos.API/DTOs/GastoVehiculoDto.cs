namespace PlayaAutos.API.DTOs
{
    public class GastoVehiculoDto
    {
        public int GastoId { get; set; }
        public int VehiculoId { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public string TipoGasto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Proveedor { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
    }

    public class CrearGastoVehiculoDto
    {
        public int VehiculoId { get; set; }
        public int TipoGastoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Proveedor { get; set; }
        public DateTime Fecha { get; set; }
        public int UsuarioRegistro { get; set; }
    }

    public class TipoGastoDto
    {
        public int TipoGastoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
