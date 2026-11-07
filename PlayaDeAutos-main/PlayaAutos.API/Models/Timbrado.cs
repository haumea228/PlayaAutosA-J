namespace PlayaAutos.API.Models
{
    public class Timbrado
    {
        public int TimbradoId { get; set; }
        public string NumeroTimbrado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public long NumeroDesde { get; set; }
        public long NumeroHasta { get; set; }
        public long UltimoNumeroUsado { get; set; } = 0;
        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public ICollection<Factura> Facturas { get; set; } = new List<Factura>();
        public ICollection<NotaCredito> NotasCredito { get; set; } = new List<NotaCredito>();
    }
}