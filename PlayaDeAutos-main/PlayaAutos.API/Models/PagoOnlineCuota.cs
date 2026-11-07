namespace PlayaAutos.API.Models
{
    /// <summary>
    /// Pago de una cuota con tarjeta mediante la pasarela (Stripe Checkout, en
    /// modo de prueba). Convive con el pago por transferencia con comprobante
    /// (SolicitudPagoCuota): este no requiere revisión del cajero porque la
    /// pasarela confirma el cobro; al confirmarse se registra con el mismo
    /// servicio de cobro (caja + ticket).
    /// </summary>
    public class PagoOnlineCuota
    {
        public int PagoOnlineCuotaId { get; set; }
        public int CuotaId { get; set; }
        public int ClienteId { get; set; }

        public string Proveedor { get; set; } = "Stripe";

        /// <summary>Id de la sesión de Checkout (cs_test_...).</summary>
        public string SesionId { get; set; } = string.Empty;

        /// <summary>Id del pago en la pasarela (pi_...), cuando se completa.</summary>
        public string? PagoProveedorId { get; set; }

        /// <summary>Página de pago de la pasarela (vence con la sesión).</summary>
        public string? UrlPago { get; set; }

        public long Monto { get; set; }
        public long MontoRecargo { get; set; }
        public string Moneda { get; set; } = "pyg";

        /// <summary>Pendiente | Pagado | Expirado | RequiereReembolso</summary>
        public string Estado { get; set; } = EstadosPagoOnline.Pendiente;

        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public DateTime? FechaConfirmacion { get; set; }

        public Cuota Cuota { get; set; } = null!;
        public Cliente Cliente { get; set; } = null!;
    }

    public static class EstadosPagoOnline
    {
        public const string Pendiente = "Pendiente";
        public const string Pagado = "Pagado";
        public const string Expirado = "Expirado";

        /// <summary>
        /// La pasarela cobró pero la cuota ya estaba pagada por otro medio
        /// (por ejemplo, en ventanilla al mismo tiempo): hay que reembolsar.
        /// </summary>
        public const string RequiereReembolso = "RequiereReembolso";
    }
}
