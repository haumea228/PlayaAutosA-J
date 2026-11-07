namespace PlayaAutos.API.DTOs
{
    public class CitaDto
    {
        public int CitaId { get; set; }
        public int ClienteId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public int? VehiculoId { get; set; }
        public string? Vehiculo { get; set; }
        public int VendedorId { get; set; }
        public string Vendedor { get; set; } = string.Empty;
        public int EstadoCitaId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? EstadoColor { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoCita { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    public class CrearCitaDto
    {
        public int ClienteId { get; set; }
        public int? VehiculoId { get; set; }
        public int VendedorId { get; set; }
        public int EstadoCitaId { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoCita { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    public class CambiarEstadoCitaDto
    {
        public int EstadoCitaId { get; set; }
    }

    public class EstadoCitaDto
    {
        public int EstadoCitaId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Color { get; set; }
    }

    public class AgendarCitaClienteDto
    {
        public int? VehiculoId { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoCita { get; set; } = "Consulta";
        public string? Observaciones { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CI_RUC { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
    }
}