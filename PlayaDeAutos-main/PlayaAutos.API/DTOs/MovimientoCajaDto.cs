namespace PlayaAutos.API.DTOs
{
    public class MovimientoCajaDto
    {
        public int MovimientoId { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Signo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? Referencia { get; set; }
        public string? Comentarios { get; set; }
        public string Usuario { get; set; } = string.Empty;
    }

    public class CrearMovimientoCajaDto
    {
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
    }

    public class BalanceCajaDto
    {
        public long TotalIngresos { get; set; }
        public long TotalEgresos { get; set; }
        public long Saldo { get; set; }
        public int CantidadMovimientos { get; set; }
    }

    public class TipoMovimientoDto
    {
        public int TipoMovimientoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Signo { get; set; } = string.Empty;
    }
    public class CierreCajaDto
    {
        public int CierreId { get; set; }
        public DateTime FechaCierre { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public long TotalIngresos { get; set; }
        public long TotalEgresos { get; set; }
        public long SaldoNeto { get; set; }
        public int CantidadMovimientos { get; set; }
        public string? Observacion { get; set; }
    }
}
