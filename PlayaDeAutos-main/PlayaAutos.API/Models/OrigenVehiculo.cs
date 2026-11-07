namespace PlayaAutos.API.Models
{
    public class OrigenVehiculo
    {
        public int OrigenId { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}