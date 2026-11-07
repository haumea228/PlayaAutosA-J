namespace PlayaAutos.API.DTOs
{
    public class NotaCreditoDto
    {
        public int NotaCreditoId { get; set; }
        public int VentaId { get; set; }
        public string NumeroNota { get; set; } = "";
        public string Timbrado { get; set; } = "";
        public DateTime FechaEmision { get; set; }
        public string Cliente { get; set; } = "";
        public string ClienteRUC { get; set; } = "";
        public string? ClienteDireccion { get; set; }
        public string? ClienteTelefono { get; set; }
        public string VehiculoVenta { get; set; } = "";
        public string? VehiculoPermuta { get; set; }
        public string Motivo { get; set; } = "";
        public long Monto { get; set; }
        public int? TasacionVehiculoId { get; set; }
        public string NumeroFactura { get; set; } = "";
        public string NumeroTimbrado { get; set; } = "";
        public DateTime TimbradoVigenciaDesde { get; set; }
        public DateTime TimbradoVigenciaHasta { get; set; }
    }

    public class NotaCreditoPdfDataDto
    {
        // Documento
        public string NumeroNota { get; set; } = "";
        public DateTime FechaEmision { get; set; }
        public string Motivo { get; set; } = "";

        // Timbrado
        public string NumeroTimbrado { get; set; } = "";
        public DateTime TimbradoVigenciaDesde { get; set; }
        public DateTime TimbradoVigenciaHasta { get; set; }

        // Cliente
        public string ClienteNombre { get; set; } = "";
        public string ClienteRUC { get; set; } = "";
        public string? ClienteDireccion { get; set; }
        public string? ClienteTelefono { get; set; }

        // Vehículo de la venta
        public string VehiculoMarca { get; set; } = "";
        public string VehiculoModelo { get; set; } = "";
        public int VehiculoAnio { get; set; }
        public string? VehiculoColor { get; set; }

        // Vehículo de la permuta (opcional)
        public string? PermutaMarca { get; set; }
        public string? PermutaModelo { get; set; }
        public int? PermutaAnio { get; set; }
        public long? PermutaKilometraje { get; set; }
        public string? PermutaEstado { get; set; }
        public string? PermutaColor { get; set; }

        // Referencia a factura de venta
        public string NumeroFactura { get; set; } = "";

        // Montos
        public long Monto { get; set; }
        public long Subtotal { get; set; }
        public long IVA { get; set; }
    }
}