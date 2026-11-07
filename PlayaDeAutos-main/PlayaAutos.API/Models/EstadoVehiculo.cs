namespace PlayaAutos.API.Models
{
    public class EstadoVehiculo
    {
        public int EstadoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool PermiteVenta { get; set; } = true;
        public bool VisibleEnCatalogo { get; set; } = true;

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}