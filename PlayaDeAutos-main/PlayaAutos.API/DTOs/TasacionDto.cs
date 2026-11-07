namespace PlayaAutos.API.DTOs
{
    public class TasacionDto
    {
        public int TasacionVehiculoId { get; set; }
        public int ClienteId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string MarcaVehiculo { get; set; } = string.Empty;
        public string ModeloVehiculo { get; set; } = string.Empty;
        public int AnhoVehiculo { get; set; }
        public long KilometrajeVehiculo { get; set; }       // ← long
        public string EstadoGeneralVehiculo { get; set; } = string.Empty;
        public string? ColorVehiculo { get; set; }
        public long ValorTasacion { get; set; }
        public long PrecioVenta { get; set; }
        public string EstadoTasacion { get; set; } = string.Empty;
        public DateTime FechaTasacion { get; set; }
        public int? ModeloId { get; set; }
        public int? TipoId { get; set; }
        public int? CondicionId { get; set; }
        public int? OrigenId { get; set; }
    }

    public class CrearTasacionDto
    {
        public int ClienteId { get; set; }
        public string MarcaVehiculo { get; set; } = string.Empty;
        public string ModeloVehiculo { get; set; } = string.Empty;
        public int AnhoVehiculo { get; set; }
        public long KilometrajeVehiculo { get; set; }       // ← long
        public string EstadoGeneralVehiculo { get; set; } = string.Empty;
        public string? ColorVehiculo { get; set; }
        public long ValorTasacion { get; set; }
        public long PrecioVenta { get; set; }
        public int? ModeloId { get; set; }
        public int? TipoId { get; set; }
        public int? CondicionId { get; set; }
        public int? OrigenId { get; set; }
    }

    public class AprobarTasacionDto
    {
        public long ValorTasacionFinal { get; set; }
        public long PrecioVentaFinal { get; set; }
        public int ModeloId { get; set; }
        public int TipoId { get; set; }
        public int CondicionId { get; set; }
        public int OrigenId { get; set; }
        public string? Color { get; set; }
        public long Kilometraje { get; set; }
    }
}