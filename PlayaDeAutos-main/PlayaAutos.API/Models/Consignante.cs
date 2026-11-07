namespace PlayaAutos.API.Models
{
    public class Consignante
    {
        public int ConsignanteId { get; set; }
        public int ClienteId { get; set; }
        public decimal PorcentajeComisionDefault { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public Cliente Cliente { get; set; } = null!;
        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
        public ICollection<ContratoConsigna> ContratosConsigna { get; set; } = new List<ContratoConsigna>();
    }
}