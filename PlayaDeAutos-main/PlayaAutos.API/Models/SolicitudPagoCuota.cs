namespace PlayaAutos.API.Models
{
    /// <summary>
    /// Pago de una cuota informado por el cliente desde la app (transferencia +
    /// foto del comprobante). No impacta caja ni marca la cuota como pagada:
    /// eso ocurre recién cuando un Cajero/Administrador la aprueba.
    /// Si se rechaza, la cuota vuelve a "Pendiente" y la solicitud queda como
    /// historial, por lo que una cuota puede tener varias solicitudes.
    /// </summary>
    public class SolicitudPagoCuota
    {
        public int SolicitudPagoCuotaId { get; set; }
        public int CuotaId { get; set; }
        public int ClienteId { get; set; }

        /// <summary>Fecha en que el cliente informó el pago (se usa como FechaPago al aprobar).</summary>
        public DateTime FechaReporte { get; set; }

        /// <summary>Monto de la cuota al momento del reporte.</summary>
        public long Monto { get; set; }

        /// <summary>Recargo por mora calculado a la fecha del reporte.</summary>
        public long MontoRecargo { get; set; }

        /// <summary>Nombre del archivo guardado por ComprobanteStorage (no es una URL pública).</summary>
        public string ComprobanteArchivo { get; set; } = string.Empty;
        public string ComprobanteContentType { get; set; } = string.Empty;

        public string? ObservacionCliente { get; set; }

        /// <summary>Pendiente | Aprobada | Rechazada</summary>
        public string Estado { get; set; } = EstadosSolicitudPago.Pendiente;

        public string? MotivoRechazo { get; set; }
        public int? UsuarioRevisionId { get; set; }
        public DateTime? FechaRevision { get; set; }

        public Cuota Cuota { get; set; } = null!;
        public Cliente Cliente { get; set; } = null!;
        public Usuario? UsuarioRevision { get; set; }
    }

    public static class EstadosSolicitudPago
    {
        public const string Pendiente = "Pendiente";
        public const string Aprobada = "Aprobada";
        public const string Rechazada = "Rechazada";
    }

    public static class EstadosCuota
    {
        public const string Pendiente = "Pendiente";
        public const string EnRevision = "EnRevision";
        public const string Pagada = "Pagada";
        public const string Anulada = "Anulada";
    }
}
