namespace PlayaAutos.API.Models
{
    public class TipoVehiculo
    {
        public int TipoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}