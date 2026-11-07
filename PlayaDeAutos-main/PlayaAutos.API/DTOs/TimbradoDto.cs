namespace PlayaAutos.API.DTOs
{
    public class TimbradoDto
    {
        public int TimbradoId { get; set; }
        public string NumeroTimbrado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public long NumeroDesde { get; set; }
        public long NumeroHasta { get; set; }
        public long UltimoNumeroUsado { get; set; }
        public bool Activo { get; set; }
    }

    public class CrearTimbradoDto
    {
        public string NumeroTimbrado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public long NumeroDesde { get; set; }
        public long NumeroHasta { get; set; }
    }
}