namespace PlayaAutos.API.Models
{
    public class Modelo
    {
        public int ModeloId { get; set; }
        public int MarcaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public Marca Marca { get; set; } = null!;
        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}