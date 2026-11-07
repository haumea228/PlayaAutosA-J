namespace PlayaAutos.API.Models
{
    public class CondicionVehiculo
    {
        public int CondicionId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}