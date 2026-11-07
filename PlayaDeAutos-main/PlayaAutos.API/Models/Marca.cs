namespace PlayaAutos.API.Models
{
    public class Marca
    {
        public int MarcaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public ICollection<Modelo> Modelos { get; set; } = new List<Modelo>();
    }
}