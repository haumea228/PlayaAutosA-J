namespace PlayaAutos.API.DTOs
{
    public class CuotaDto
    {
        public int CuotaId { get; set; }
        public int VentaId { get; set; }
        public int NumeroCuota { get; set; }
        public long Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public long? MontoPagado { get; set; }
        public long? MontoRecargo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;
        public int? CantidadCuotas { get; set; }
        public decimal? RecargoAutomatico { get; set; }
        public string? ComprobanteImagen { get; set; }

        /// <summary>
        /// Solicitud de pago (app) relacionada: la Pendiente si la cuota está
        /// EnRevision, o la Aprobada si se pagó por ese medio. Permite a la web
        /// mostrar el comprobante, que ya no viaja embebido en ComprobanteImagen.
        /// </summary>
        public int? SolicitudPagoId { get; set; }
    }

    /// <summary>Cuota tal como la ve el cliente en la app (GET api/micuenta/cuotas).</summary>
    public class MiCuotaDto
    {
        public int CuotaId { get; set; }
        public int VentaId { get; set; }
        public int NumeroCuota { get; set; }
        public int? CantidadCuotas { get; set; }
        public long Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public long? MontoPagado { get; set; }
        public long? MontoRecargo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Vehiculo { get; set; } = string.Empty;

        /// <summary>Recargo por mora si se informara el pago hoy (0 si no está vencida).</summary>
        public long RecargoCalculado { get; set; }
        /// <summary>Monto + recargo: lo que el cliente debe transferir hoy.</summary>
        public long MontoAPagar { get; set; }

        /// <summary>True solo para la cuota pendiente más antigua de la venta, sin pago en revisión.</summary>
        public bool PuedeInformarPago { get; set; }
        /// <summary>Por qué no se puede informar el pago (null si se puede o si ya está pagada).</summary>
        public string? MotivoNoPuedePagar { get; set; }

        /// <summary>La pasarela de pago con tarjeta (Stripe) está configurada.</summary>
        public bool PagoConTarjetaDisponible { get; set; }

        public string? UltimaSolicitudEstado { get; set; }
        public DateTime? UltimaSolicitudFecha { get; set; }
        public string? UltimaSolicitudMotivoRechazo { get; set; }
    }

    /// <summary>Pago de cuota informado por el cliente, visto por Cajero/Administrador.</summary>
    public class SolicitudPagoCuotaDto
    {
        public int SolicitudPagoCuotaId { get; set; }
        public int CuotaId { get; set; }
        public int VentaId { get; set; }
        public int NumeroCuota { get; set; }
        public int? CantidadCuotas { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string? ClienteCI { get; set; }
        public string? ClienteTelefono { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public DateTime FechaVencimiento { get; set; }
        public DateTime FechaReporte { get; set; }
        public long Monto { get; set; }
        public long MontoRecargo { get; set; }
        public long Total { get; set; }
        public string? ObservacionCliente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
        public string? RevisadoPor { get; set; }
        public DateTime? FechaRevision { get; set; }
    }

    /// <summary>Respuesta al iniciar o verificar un pago con tarjeta.</summary>
    public class PagoOnlineDto
    {
        public int PagoOnlineId { get; set; }
        public int CuotaId { get; set; }
        /// <summary>Página de pago de Stripe a abrir en el navegador.</summary>
        public string? Url { get; set; }
        public long Total { get; set; }
        /// <summary>Pendiente | Pagado | Expirado | RequiereReembolso</summary>
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
    }

    public class AprobarSolicitudPagoDto
    {
        public int FormaPagoId { get; set; }
        public string? ObservacionPago { get; set; }
    }

    public class RechazarSolicitudPagoDto
    {
        public string Motivo { get; set; } = string.Empty;
    }

    public class PagarCuotaDto
    {
        public DateTime FechaPago { get; set; }
        public long MontoPagado { get; set; }
        public long? MontoRecargo { get; set; }
        public int FormaPagoId { get; set; }
        public int UsuarioRegistro { get; set; }
        public string? ObservacionPago { get; set; }
        public string? ComprobanteImagen { get; set; }
    }
}