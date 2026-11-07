namespace PlayaAutos.API.Models
{
    public class ContratoConsigna
    {
        public int ContratoId { get; set; }
        public int VehiculoId { get; set; }
        public int ConsignanteId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal PorcentajeComision { get; set; }
        public string? Clausulas { get; set; }
        public string? RutaPDF { get; set; }
        public string Estado { get; set; } = "Vigente";
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public int? TasacionVehiculoId { get; set; }

        public Vehiculo Vehiculo { get; set; } = null!;
        public Consignante Consignante { get; set; } = null!;
        public TasacionVehiculo? Tasacion { get; set; }
    }
}