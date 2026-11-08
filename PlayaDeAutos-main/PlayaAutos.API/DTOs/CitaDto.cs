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

        /// <summary>
        /// Opcional, solo al cancelar: se agrega a la descripción de la cita
        /// (la ve el cliente en la app) y va en la notificación push.
        /// </summary>
        public string? Motivo { get; set; }
    }

    /// <summary>Cancelación hecha por el cliente desde la app.</summary>
    public class CancelarCitaClienteDto
    {
        /// <summary>Opcional: queda en la descripción para que lo vea el encargado.</summary>
        public string? Motivo { get; set; }
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

        // Datos de contacto: OPCIONALES. La app ya no los envía (se editan solo
        // desde Perfil y se usan los del perfil); el portal web de clientes sí
        // los manda y, si vienen, se actualizan en el perfil como antes.
        public string? Nombre { get; set; }
        public string? CI_RUC { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }
}