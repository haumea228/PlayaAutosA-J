namespace PlayaAutos.API.Models
{
    public class Cita
    {
        public int CitaId { get; set; }
        public int ClienteId { get; set; }
        public int? VehiculoId { get; set; }
        public int VendedorId { get; set; }
        public int EstadoCitaId { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoCita { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public Cliente Cliente { get; set; } = null!;
        public Vehiculo? Vehiculo { get; set; }
        public EstadoCita EstadoCita { get; set; } = null!;
    }
}