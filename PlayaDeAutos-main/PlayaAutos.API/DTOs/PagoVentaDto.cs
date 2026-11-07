namespace PlayaAutos.API.DTOs
{
    public class PagoVentaDto
    {
        public int PagoVentaId { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public long Monto { get; set; }
        public string? ComprobanteImagen { get; set; }
        public string? Observacion { get; set; }
        public int? TasacionVehiculoId { get; set; }
        public string? VehiculoTasado { get; set; }
    }

    public class CrearPagoVentaDto
    {
        public int FormaPagoId { get; set; }
        public long Monto { get; set; }
        public string? ComprobanteImagen { get; set; }
        public string? Observacion { get; set; }
        public int? TasacionVehiculoId { get; set; }
    }
}