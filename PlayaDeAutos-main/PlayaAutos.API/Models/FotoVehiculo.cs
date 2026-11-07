namespace PlayaAutos.API.Models
{
    public class FotoVehiculo
    {
        public int FotoId { get; set; }
        public int VehiculoId { get; set; }
        public string URL { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public bool EsPrincipal { get; set; } = false;
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public Vehiculo Vehiculo { get; set; } = null!;
    }
}