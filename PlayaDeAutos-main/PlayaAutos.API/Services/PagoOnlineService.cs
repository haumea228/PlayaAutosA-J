using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlayaAutos.API.Data;
using PlayaAutos.API.Models;
using Stripe;
using Stripe.Checkout;

namespace PlayaAutos.API.Services
{
    /// <summary>Sección "Pagos" de appsettings.</summary>
    public class PagosOptions
    {
        /// <summary>Archivo con la clave secreta de Stripe (sk_test_...), relativo al ContentRoot.</summary>
        public string? StripeSecretKeyPath { get; set; }

        /// <summary>
        /// Base de las URLs de retorno de Stripe, tal como la ve el navegador del
        /// teléfono (en el emulador de Android, la PC es 10.0.2.2).
        /// </summary>
        public string UrlRetornoBase { get; set; } = "http://10.0.2.2:5035";

        /// <summary>
        /// Enlace de la app (deep link) al que vuelve Stripe al terminar el pago,
        /// p. ej. "playaautos://pago". La app cierra la pestaña del navegador y
        /// verifica el cobro. Si se deja vacío, Stripe vuelve a las páginas de la
        /// API (UrlRetornoBase/api/pagosonline/...).
        /// </summary>
        public string? UrlRetornoApp { get; set; }

        public string Moneda { get; set; } = "pyg";

        /// <summary>Validez de la página de pago (Stripe exige entre 30 minutos y 24 horas).</summary>
        public int MinutosExpiracion { get; set; } = 60;
    }

    /// <summary>
    /// Cliente de Stripe (singleton). Si no hay clave configurada queda
    /// deshabilitado y la app solo ofrece el pago por transferencia.
    /// </summary>
    public class StripeGateway
    {
        public SessionService? Sesiones { get; }
        public bool Habilitado => Sesiones != null;

        public StripeGateway(IOptions<PagosOptions> opciones, IWebHostEnvironment env, ILogger<StripeGateway> logger)
        {
            var ruta = opciones.Value.StripeSecretKeyPath;
            if (string.IsNullOrWhiteSpace(ruta))
            {
                logger.LogWarning("Pago con tarjeta deshabilitado: falta Pagos:StripeSecretKeyPath.");
                return;
            }

            var completa = Path.IsPathRooted(ruta) ? ruta : Path.Combine(env.ContentRootPath, ruta);
            if (!System.IO.File.Exists(completa))
            {
                logger.LogWarning("Pago con tarjeta deshabilitado: no se encontró la clave de Stripe en {Ruta}.", completa);
                return;
            }

            var clave = System.IO.File.ReadAllText(completa).Trim();
            if (!clave.StartsWith("sk_test_"))
            {
                // Proyecto académico: solo se permite el entorno de pruebas.
                logger.LogError("Pago con tarjeta deshabilitado: la clave de Stripe no es de prueba (sk_test_).");
                return;
            }

            Sesiones = new SessionService(new StripeClient(clave));
        }
    }

    /// <summary>
    /// Pago de cuotas con tarjeta vía Stripe Checkout. La app abre la página de
    /// pago de Stripe; al volver, la API consulta a Stripe si se cobró y, en ese
    /// caso, registra el cobro con CuotaPagoService (cuota pagada, ingreso en
    /// caja, ticket) y avisa al cliente por push. No hacen falta webhooks: la
    /// confirmación siempre se verifica del lado del servidor contra Stripe.
    /// </summary>
    public class PagoOnlineService
    {
        public const string DescripcionFormaPago = "Tarjeta online (Stripe)";

        private readonly AppDbContext _context;
        private readonly StripeGateway _stripe;
        private readonly CuotaPagoService _pagoService;
        private readonly NotificacionesService _notificaciones;
        private readonly PagosOptions _opciones;
        private readonly ILogger<PagoOnlineService> _logger;

        public PagoOnlineService(AppDbContext context, StripeGateway stripe, CuotaPagoService pagoService,
            NotificacionesService notificaciones, IOptions<PagosOptions> opciones, ILogger<PagoOnlineService> logger)
        {
            _context = context;
            _stripe = stripe;
            _pagoService = pagoService;
            _notificaciones = notificaciones;
            _opciones = opciones.Value;
            _logger = logger;
        }

        public bool Habilitado => _stripe.Habilitado;

        /// <summary>Crea la página de pago de Stripe para la cuota. Devuelve el pago o un error para el cliente.</summary>
        public async Task<(PagoOnlineCuota? Pago, string? Error)> IniciarAsync(Cliente cliente, int cuotaId, CancellationToken ct)
        {
            if (!Habilitado) return (null, "El pago con tarjeta no está disponible en este momento.");

            // Si quedó una página de pago abierta para esta cuota, primero se
            // verifica (quizás ya se pagó) y si no, se cierra para evitar cobros dobles.
            await CerrarPendientesAsync(cuotaId, ct);

            var cuota = await _context.Cuotas
                .Include(c => c.Venta).ThenInclude(v => v.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .FirstOrDefaultAsync(c => c.CuotaId == cuotaId && c.Venta.ClienteId == cliente.ClienteId, ct);
            if (cuota == null) return (null, "Cuota no encontrada.");

            var error = await _pagoService.ValidarPagoClienteAsync(cuota);
            if (error != null) return (null, error);

            var ahora = DateTime.Now;
            var recargo = CuotaPagoService.CalcularRecargo(cuota, cuota.Venta, ahora);
            var total = cuota.Monto + recargo;
            var vehiculo = $"{cuota.Venta.Vehiculo.Modelo.Marca.Nombre} {cuota.Venta.Vehiculo.Modelo.Nombre}";
            var deTotal = cuota.Venta.CantidadCuotas.HasValue ? $" de {cuota.Venta.CantidadCuotas}" : "";
            var baseUrl = _opciones.UrlRetornoBase.TrimEnd('/');

            var opciones = new SessionCreateOptions
            {
                Mode = "payment",
                Locale = "es",
                ClientReferenceId = cuota.CuotaId.ToString(),
                CustomerEmail = EsEmail(cliente.Email) ? cliente.Email : null,
                ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Clamp(_opciones.MinutosExpiracion, 30, 1440)),
                SuccessUrl = string.IsNullOrWhiteSpace(_opciones.UrlRetornoApp)
                    ? $"{baseUrl}/api/pagosonline/retorno?session_id={{CHECKOUT_SESSION_ID}}"
                    : $"{_opciones.UrlRetornoApp.TrimEnd('/')}/completado?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = string.IsNullOrWhiteSpace(_opciones.UrlRetornoApp)
                    ? $"{baseUrl}/api/pagosonline/cancelado"
                    : $"{_opciones.UrlRetornoApp.TrimEnd('/')}/cancelado",
                Metadata = new Dictionary<string, string>
                {
                    ["cuotaId"] = cuota.CuotaId.ToString(),
                    ["clienteId"] = cliente.ClienteId.ToString()
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _opciones.Moneda,
                            UnitAmount = total, // PYG es moneda sin decimales: el monto va tal cual.
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Cuota {cuota.NumeroCuota}{deTotal} — {vehiculo}",
                                Description = recargo > 0
                                    ? $"Incluye recargo por mora de Gs. {recargo:N0}"
                                    : $"A&J Autos Usados — Venta #{cuota.VentaId}"
                            }
                        }
                    }
                }
            };

            // Los medios de pago los decide Stripe según el panel de la cuenta y la
            // moneda (la API actual ya no acepta payment_method_types). En guaraníes
            // solo ofrece tarjeta; las transferencias siguen por el flujo de
            // comprobante con revisión del cajero.
            Session sesion;
            try
            {
                sesion = await _stripe.Sesiones!.CreateAsync(opciones, cancellationToken: ct);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe rechazó la creación del pago de la cuota {CuotaId}.", cuotaId);
                return (null, "No se pudo iniciar el pago con tarjeta. Probá de nuevo en unos minutos.");
            }

            var pago = new PagoOnlineCuota
            {
                CuotaId = cuota.CuotaId,
                ClienteId = cliente.ClienteId,
                Proveedor = "Stripe",
                SesionId = sesion.Id,
                UrlPago = sesion.Url,
                Monto = cuota.Monto,
                MontoRecargo = recargo,
                Moneda = _opciones.Moneda,
                Estado = EstadosPagoOnline.Pendiente,
                FechaCreacion = ahora,
                FechaExpiracion = sesion.ExpiresAt.ToLocalTime()
            };
            _context.PagosOnlineCuota.Add(pago);
            await _context.SaveChangesAsync(ct);
            return (pago, null);
        }

        /// <summary>Consulta a Stripe el estado del pago y, si se cobró, registra el pago de la cuota.</summary>
        public async Task<PagoOnlineCuota> ConfirmarAsync(int pagoId, CancellationToken ct)
        {
            var pago = await _context.PagosOnlineCuota.FirstAsync(p => p.PagoOnlineCuotaId == pagoId, ct);
            if (pago.Estado != EstadosPagoOnline.Pendiente || !Habilitado) return pago;

            var sesion = await _stripe.Sesiones!.GetAsync(pago.SesionId, cancellationToken: ct);

            if (sesion.PaymentStatus == "paid")
                return await AplicarCobroAsync(pago.PagoOnlineCuotaId, sesion.PaymentIntentId, ct);

            if (sesion.Status == "expired")
            {
                pago.Estado = EstadosPagoOnline.Expirado;
                await _context.SaveChangesAsync(ct);
            }
            return pago;
        }

        public async Task<PagoOnlineCuota?> ConfirmarPorSesionAsync(string sesionId, CancellationToken ct)
        {
            var id = await _context.PagosOnlineCuota
                .Where(p => p.SesionId == sesionId)
                .Select(p => (int?)p.PagoOnlineCuotaId)
                .FirstOrDefaultAsync(ct);
            return id == null ? null : await ConfirmarAsync(id.Value, ct);
        }

        /// <summary>
        /// Verifica los pagos con tarjeta que siguen pendientes del cliente (por
        /// ejemplo, pagó y cerró la app antes de volver). Nunca lanza excepciones.
        /// </summary>
        public async Task ConciliarPendientesAsync(int clienteId, CancellationToken ct)
        {
            if (!Habilitado) return;

            var limite = DateTime.Now.AddDays(-2);
            var pendientes = await _context.PagosOnlineCuota
                .Where(p => p.ClienteId == clienteId && p.Estado == EstadosPagoOnline.Pendiente && p.FechaCreacion > limite)
                .Select(p => p.PagoOnlineCuotaId)
                .ToListAsync(ct);

            foreach (var id in pendientes)
            {
                try { await ConfirmarAsync(id, ct); }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "No se pudo verificar el pago online {PagoId}.", id);
                }
            }
        }

        /// <summary>
        /// Antes de otro intento de pago (tarjeta o transferencia): verifica las
        /// páginas de pago abiertas de la cuota y cierra en Stripe las que no se pagaron.
        /// </summary>
        public async Task CerrarPendientesAsync(int cuotaId, CancellationToken ct)
        {
            if (!Habilitado) return;

            var pendientes = await _context.PagosOnlineCuota
                .Where(p => p.CuotaId == cuotaId && p.Estado == EstadosPagoOnline.Pendiente)
                .Select(p => p.PagoOnlineCuotaId)
                .ToListAsync(ct);

            foreach (var id in pendientes)
            {
                var pago = await ConfirmarAsync(id, ct);
                if (pago.Estado != EstadosPagoOnline.Pendiente) continue;

                try
                {
                    await _stripe.Sesiones!.ExpireAsync(pago.SesionId, cancellationToken: ct);
                    pago.Estado = EstadosPagoOnline.Expirado;
                    await _context.SaveChangesAsync(ct);
                }
                catch (StripeException)
                {
                    // Se completó justo ahora: se registra el cobro.
                    await ConfirmarAsync(id, ct);
                }
            }
        }

        private async Task<PagoOnlineCuota> AplicarCobroAsync(int pagoId, string? pagoProveedorId, CancellationToken ct)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var pago = await _context.PagosOnlineCuota.FirstAsync(p => p.PagoOnlineCuotaId == pagoId, ct);
                if (pago.Estado != EstadosPagoOnline.Pendiente) return pago;

                var cuota = await _context.Cuotas
                    .Include(c => c.Venta)
                    .FirstAsync(c => c.CuotaId == pago.CuotaId, ct);
                var usuarioCliente = await _context.Clientes
                    .Where(c => c.ClienteId == pago.ClienteId)
                    .Select(c => c.UsuarioId)
                    .FirstOrDefaultAsync(ct);

                pago.PagoProveedorId = pagoProveedorId;
                pago.FechaConfirmacion = DateTime.Now;

                if (cuota.Estado is EstadosCuota.Pagada or EstadosCuota.Anulada)
                {
                    // Stripe cobró, pero la cuota ya se había saldado (o anulado) por otro medio.
                    pago.Estado = EstadosPagoOnline.RequiereReembolso;
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    _logger.LogWarning("Pago online {PagoId} cobrado sobre la cuota {CuotaId} en estado {Estado}: requiere reembolso.",
                        pago.PagoOnlineCuotaId, cuota.CuotaId, cuota.Estado);
                    return pago;
                }

                // Había un comprobante de transferencia en revisión: el pago con
                // tarjeta lo reemplaza, así el cajero no lo aprueba por duplicado.
                if (cuota.Estado == EstadosCuota.EnRevision)
                {
                    var abiertas = await _context.SolicitudesPagoCuota
                        .Where(s => s.CuotaId == cuota.CuotaId && s.Estado == EstadosSolicitudPago.Pendiente)
                        .ToListAsync(ct);
                    foreach (var s in abiertas)
                    {
                        s.Estado = EstadosSolicitudPago.Rechazada;
                        s.MotivoRechazo = "La cuota se pagó con tarjeta.";
                        s.FechaRevision = DateTime.Now;
                    }
                }

                var formaPagoId = await _context.FormasPago
                    .Where(f => f.Descripcion == DescripcionFormaPago)
                    .Select(f => (int?)f.FormaPagoId)
                    .FirstOrDefaultAsync(ct)
                    ?? throw new InvalidOperationException($"No existe la forma de pago '{DescripcionFormaPago}'.");

                var error = await _pagoService.RegistrarPagoAsync(cuota, new RegistroPagoCuota(
                    DateTime.Now,
                    pago.Monto,
                    pago.MontoRecargo,
                    formaPagoId,
                    usuarioCliente ?? 0,
                    $"Pago con tarjeta vía Stripe ({pagoProveedorId})",
                    null));
                if (error != null) throw new InvalidOperationException(error);

                pago.Estado = EstadosPagoOnline.Pagado;
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await NotificarAsync(usuarioCliente, cuota);
                return pago;
            }
            catch (DbUpdateException)
            {
                // Otro request (la página de retorno y la app a la vez) ya
                // registró este cobro: se descarta este intento y se relee.
                await tx.RollbackAsync(ct);
                _context.ChangeTracker.Clear();
                return await _context.PagosOnlineCuota.AsNoTracking().FirstAsync(p => p.PagoOnlineCuotaId == pagoId, ct);
            }
        }

        private async Task NotificarAsync(int? usuarioId, Cuota cuota)
        {
            var vehiculo = await _context.Ventas
                .Where(v => v.VentaId == cuota.VentaId)
                .Select(v => v.Vehiculo.Modelo.Marca.Nombre + " " + v.Vehiculo.Modelo.Nombre)
                .FirstOrDefaultAsync();
            await _notificaciones.NotificarUsuarioAsync(usuarioId,
                "Pago confirmado",
                $"Recibimos el pago con tarjeta de tu cuota {cuota.NumeroCuota} ({vehiculo}). Ya podés descargar el ticket desde la app.",
                "cuota", cuota.CuotaId);
        }

        private static bool EsEmail(string? s) =>
            !string.IsNullOrWhiteSpace(s) && System.Net.Mail.MailAddress.TryCreate(s, out _);
    }
}
