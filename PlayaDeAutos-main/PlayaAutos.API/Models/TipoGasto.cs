namespace PlayaAutos.API.Models
{
    public class TipoGasto
    {
        public int TipoGastoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public ICollection<GastoVehiculo> GastosVehiculo { get; set; } = new List<GastoVehiculo>();
    }
}