namespace PlayaAutos.API.Models
{
    public class EstadoCita
    {
        public int EstadoCitaId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Color { get; set; }

        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    }
}