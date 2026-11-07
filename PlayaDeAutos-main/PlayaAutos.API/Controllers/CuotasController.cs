using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Models;
using PlayaAutos.API.Services;
using System.Security.Claims;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor,Cajero")]
    public class CuotasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CuotaPagoService _pagoService;
        private readonly ComprobanteStorage _comprobantes;
        private readonly NotificacionesService _notificaciones;
        private readonly PagoOnlineService _pagosOnline;

        public CuotasController(AppDbContext context, CuotaPagoService pagoService, ComprobanteStorage comprobantes,
            NotificacionesService notificaciones, PagoOnlineService pagosOnline)
        {
            _context = context;
            _pagoService = pagoService;
            _comprobantes = comprobantes;
            _notificaciones = notificaciones;
            _pagosOnline = pagosOnline;
        }

        /// <summary>"cuota 4 de 6 (Toyota Fielder)" para los textos de las notificaciones.</summary>
        private async Task<(int? UsuarioId, string Descripcion)> DatosNotificacionAsync(int solicitudId)
        {
            var d = await _context.SolicitudesPagoCuota
                .Where(s => s.SolicitudPagoCuotaId == solicitudId)
                .Select(s => new
                {
                    s.Cliente.UsuarioId,
                    s.Cuota.NumeroCuota,
                    s.Cuota.Venta.CantidadCuotas,
                    Vehiculo = s.Cuota.Venta.Vehiculo.Modelo.Marca.Nombre + " " + s.Cuota.Venta.Vehiculo.Modelo.Nombre
                })
                .FirstAsync();

            var de = d.CantidadCuotas.HasValue ? $" de {d.CantidadCuotas}" : "";
            return (d.UsuarioId, $"cuota {d.NumeroCuota}{de} ({d.Vehiculo})");
        }

        private int? GetUsuarioId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        // GET: api/cuotas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CuotaDto>>> GetCuotas([FromQuery] string? estado)
        {
            var query = _context.Cuotas
                .Include(c => c.Venta).ThenInclude(v => v.Cliente)
                .Include(c => c.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Where(c => c.Venta.Estado != "Anulada")
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "Vencida")
                {
                    // Vencidas = Pendientes con fecha de vencimiento pasada
                    query = query.Where(c => c.Estado == EstadosCuota.Pendiente && c.FechaVencimiento < DateTime.Now);
                }
                else
                {
                    query = query.Where(c => c.Estado == estado);
                }
            }

            return await query
                .OrderBy(c => c.FechaVencimiento)
                .Select(c => new CuotaDto
                {
                    CuotaId = c.CuotaId,
                    VentaId = c.VentaId,
                    NumeroCuota = c.NumeroCuota,
                    Monto = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    FechaPago = c.FechaPago,
                    MontoPagado = c.MontoPagado,
                    MontoRecargo = c.MontoRecargo,
                    Estado = c.Estado,
                    Cliente = c.Venta.Cliente.Nombre,
                    Vehiculo = c.Venta.Vehiculo.Modelo.Marca.Nombre + " " + c.Venta.Vehiculo.Modelo.Nombre,
                    CantidadCuotas = c.Venta.CantidadCuotas,
                    ComprobanteImagen = c.ComprobanteImagen,
                    SolicitudPagoId = c.SolicitudesPago
                        .Where(s => s.Estado == EstadosSolicitudPago.Pendiente || s.Estado == EstadosSolicitudPago.Aprobada)
                        .OrderByDescending(s => s.SolicitudPagoCuotaId)
                        .Select(s => (int?)s.SolicitudPagoCuotaId)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        // GET: api/cuotas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CuotaDto>> GetCuota(int id)
        {
            var c = await _context.Cuotas
                .Include(c => c.Venta).ThenInclude(v => v.Cliente)
                .Include(c => c.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .FirstOrDefaultAsync(c => c.CuotaId == id);

            if (c == null) return NotFound();

            //Calcular recargo automático si la cuota está vencida
            decimal? recargoAuto = null;
            if (c.Estado == EstadosCuota.Pendiente)
            {
                var recargo = CuotaPagoService.CalcularRecargo(c, c.Venta, DateTime.Now);
                if (recargo > 0) recargoAuto = recargo;
            }

            return new CuotaDto
            {
                CuotaId = c.CuotaId,
                VentaId = c.VentaId,
                NumeroCuota = c.NumeroCuota,
                Monto = c.Monto,
                FechaVencimiento = c.FechaVencimiento,
                FechaPago = c.FechaPago,
                MontoPagado = c.MontoPagado,
                MontoRecargo = c.MontoRecargo,
                Estado = c.Estado,
                Cliente = c.Venta.Cliente.Nombre,
                Vehiculo = c.Venta.Vehiculo.Modelo.Marca.Nombre + " " + c.Venta.Vehiculo.Modelo.Nombre,
                CantidadCuotas = c.Venta.CantidadCuotas,
                RecargoAutomatico = recargoAuto,
                ComprobanteImagen = c.ComprobanteImagen
            };
        }

        // GET: api/cuotas/venta/5
        [HttpGet("venta/{ventaId}")]
        public async Task<ActionResult<IEnumerable<CuotaDto>>> GetCuotasPorVenta(int ventaId)
        {
            return await _context.Cuotas
                .Where(c => c.VentaId == ventaId)
                .OrderBy(c => c.NumeroCuota)
                .Select(c => new CuotaDto
                {
                    CuotaId = c.CuotaId,
                    NumeroCuota = c.NumeroCuota,
                    Monto = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    FechaPago = c.FechaPago,
                    MontoPagado = c.MontoPagado,
                    MontoRecargo = c.MontoRecargo,
                    Estado = c.Estado,
                    RecargoAutomatico = null
                })
                .ToListAsync();
        }

        // PUT: api/cuotas/5/pagar  — cobro directo en ventanilla (sin cambios funcionales)
        [HttpPut("{id}/pagar")]
        public async Task<IActionResult> PagarCuota(int id, [FromBody] PagarCuotaDto dto)
        {
            // Si el cliente dejó abierta una página de pago con tarjeta, se verifica
            // (quizás ya pagó) y se cierra, para no cobrar dos veces la misma cuota.
            try { await _pagosOnline.CerrarPendientesAsync(id, HttpContext.RequestAborted); }
            catch (Stripe.StripeException) { /* sin conexión con Stripe: se sigue con el cobro en ventanilla */ }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cuota = await _context.Cuotas
                    .Include(c => c.Venta)
                    .FirstOrDefaultAsync(c => c.CuotaId == id);

                if (cuota == null) return NotFound();
                if (cuota.Estado == EstadosCuota.EnRevision)
                    return BadRequest("El cliente informó un pago de esta cuota que está en revisión. Aprobalo o rechazalo desde Pendientes.");

                var error = await _pagoService.RegistrarPagoAsync(cuota, new RegistroPagoCuota(
                    dto.FechaPago,
                    dto.MontoPagado,
                    dto.MontoRecargo ?? 0,
                    dto.FormaPagoId,
                    GetUsuarioId() ?? dto.UsuarioRegistro,
                    dto.ObservacionPago,
                    dto.ComprobanteImagen));

                if (error != null) return BadRequest(error);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Cuota pagada correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al pagar la cuota: {ex.Message}");
            }
        }

        // ── Pagos informados por el cliente desde la app ─────────────────────

        // GET: api/cuotas/solicitudes?estado=Pendiente
        [HttpGet("solicitudes")]
        [Authorize(Roles = "AdministradorP,Cajero")]
        public async Task<ActionResult<IEnumerable<SolicitudPagoCuotaDto>>> GetSolicitudes([FromQuery] string? estado = EstadosSolicitudPago.Pendiente)
        {
            var query = _context.SolicitudesPagoCuota.AsQueryable();
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(s => s.Estado == estado);

            return await query
                .OrderBy(s => s.FechaReporte)
                .Select(s => new SolicitudPagoCuotaDto
                {
                    SolicitudPagoCuotaId = s.SolicitudPagoCuotaId,
                    CuotaId = s.CuotaId,
                    VentaId = s.Cuota.VentaId,
                    NumeroCuota = s.Cuota.NumeroCuota,
                    CantidadCuotas = s.Cuota.Venta.CantidadCuotas,
                    Cliente = s.Cliente.Nombre,
                    ClienteCI = s.Cliente.CI_RUC,
                    ClienteTelefono = s.Cliente.Telefono,
                    Vehiculo = s.Cuota.Venta.Vehiculo.Modelo.Marca.Nombre + " " + s.Cuota.Venta.Vehiculo.Modelo.Nombre,
                    FechaVencimiento = s.Cuota.FechaVencimiento,
                    FechaReporte = s.FechaReporte,
                    Monto = s.Monto,
                    MontoRecargo = s.MontoRecargo,
                    Total = s.Monto + s.MontoRecargo,
                    ObservacionCliente = s.ObservacionCliente,
                    Estado = s.Estado,
                    MotivoRechazo = s.MotivoRechazo,
                    RevisadoPor = s.UsuarioRevision != null ? s.UsuarioRevision.UsuarioNombre : null,
                    FechaRevision = s.FechaRevision
                })
                .ToListAsync();
        }

        // GET: api/cuotas/solicitudes/5/comprobante
        [HttpGet("solicitudes/{id}/comprobante")]
        public async Task<IActionResult> GetComprobanteSolicitud(int id)
        {
            var solicitud = await _context.SolicitudesPagoCuota
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SolicitudPagoCuotaId == id);
            if (solicitud == null) return NotFound();

            var ruta = _comprobantes.ObtenerRuta(solicitud.ComprobanteArchivo);
            if (ruta == null) return NotFound("No se encontró el archivo del comprobante.");

            return PhysicalFile(ruta, solicitud.ComprobanteContentType);
        }

        // PUT: api/cuotas/solicitudes/5/aprobar
        [HttpPut("solicitudes/{id}/aprobar")]
        [Authorize(Roles = "AdministradorP,Cajero")]
        public async Task<IActionResult> AprobarSolicitud(int id, [FromBody] AprobarSolicitudPagoDto dto)
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == null) return Unauthorized();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var solicitud = await _context.SolicitudesPagoCuota
                    .Include(s => s.Cuota).ThenInclude(c => c.Venta)
                    .FirstOrDefaultAsync(s => s.SolicitudPagoCuotaId == id);

                if (solicitud == null) return NotFound();
                if (solicitud.Estado != EstadosSolicitudPago.Pendiente)
                    return BadRequest($"La solicitud ya fue procesada (estado: {solicitud.Estado}).");

                // El pago se registra con la fecha y el recargo del momento en que
                // el cliente lo informó (ya había transferido ese día).
                var error = await _pagoService.RegistrarPagoAsync(solicitud.Cuota, new RegistroPagoCuota(
                    solicitud.FechaReporte,
                    solicitud.Monto,
                    solicitud.MontoRecargo,
                    dto.FormaPagoId,
                    usuarioId.Value,
                    string.IsNullOrWhiteSpace(dto.ObservacionPago)
                        ? "Pago informado por el cliente desde la app"
                        : dto.ObservacionPago.Trim(),
                    null));

                if (error != null) return BadRequest(error);

                solicitud.Estado = EstadosSolicitudPago.Aprobada;
                solicitud.UsuarioRevisionId = usuarioId;
                solicitud.FechaRevision = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var (clienteUsuarioId, descripcion) = await DatosNotificacionAsync(id);
                await _notificaciones.NotificarUsuarioAsync(clienteUsuarioId,
                    "Pago confirmado",
                    $"Confirmamos el pago de tu {descripcion}. Ya podés descargar el ticket desde la app.",
                    "cuota", solicitud.CuotaId);

                return Ok(new { mensaje = "Pago aprobado. La cuota quedó registrada como pagada.", solicitud.CuotaId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al aprobar el pago: {ex.Message}");
            }
        }

        // PUT: api/cuotas/solicitudes/5/rechazar
        [HttpPut("solicitudes/{id}/rechazar")]
        [Authorize(Roles = "AdministradorP,Cajero")]
        public async Task<IActionResult> RechazarSolicitud(int id, [FromBody] RechazarSolicitudPagoDto dto)
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var motivo = dto.Motivo?.Trim() ?? "";
            if (motivo.Length == 0) return BadRequest("Ingresá el motivo del rechazo.");
            if (motivo.Length > 500) return BadRequest("El motivo no puede superar los 500 caracteres.");

            var solicitud = await _context.SolicitudesPagoCuota
                .Include(s => s.Cuota)
                .FirstOrDefaultAsync(s => s.SolicitudPagoCuotaId == id);

            if (solicitud == null) return NotFound();
            if (solicitud.Estado != EstadosSolicitudPago.Pendiente)
                return BadRequest($"La solicitud ya fue procesada (estado: {solicitud.Estado}).");

            solicitud.Estado = EstadosSolicitudPago.Rechazada;
            solicitud.MotivoRechazo = motivo;
            solicitud.UsuarioRevisionId = usuarioId;
            solicitud.FechaRevision = DateTime.Now;

            // La cuota vuelve a quedar disponible para que el cliente informe otro pago.
            if (solicitud.Cuota.Estado == EstadosCuota.EnRevision)
                solicitud.Cuota.Estado = EstadosCuota.Pendiente;

            await _context.SaveChangesAsync();

            var (clienteUsuarioId, descripcion) = await DatosNotificacionAsync(id);
            await _notificaciones.NotificarUsuarioAsync(clienteUsuarioId,
                "Pago rechazado",
                $"No pudimos confirmar el pago de tu {descripcion}: {motivo}. Podés enviar un nuevo comprobante.",
                "cuota", solicitud.CuotaId);

            return Ok(new { mensaje = "Pago rechazado.", solicitud.CuotaId });
        }

        // GET: api/cuotas/5/ticket
        [HttpGet("{id}/ticket")]
        public async Task<IActionResult> DescargarTicket(int id)
        {
            var cuota = await _context.Cuotas
                .Include(c => c.Venta).ThenInclude(v => v.Cliente)
                .Include(c => c.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(c => c.Venta).ThenInclude(v => v.Cuotas)
                .Include(c => c.Venta).ThenInclude(v => v.Vendedor)
                .Include(c => c.FormaPago)
                .FirstOrDefaultAsync(c => c.CuotaId == id);

            if (cuota == null) return NotFound();
            if (cuota.Estado != EstadosCuota.Pagada) return BadRequest("La cuota aún no ha sido pagada.");

            var venta = cuota.Venta;
            var saldoInicial = venta.SaldoFinanciado ?? venta.MontoTotal;

            var pagosAnteriores = venta.Cuotas
                .Where(c => c.Estado == EstadosCuota.Pagada && c.NumeroCuota < cuota.NumeroCuota)
                .Sum(c => (c.MontoPagado ?? 0) + (c.MontoRecargo ?? 0));

            var saldoAnterior = saldoInicial - pagosAnteriores;
            var abono = cuota.MontoPagado ?? cuota.Monto;
            var recargo = cuota.MontoRecargo ?? 0;
            var saldoActual = saldoAnterior - abono;

            var data = new TicketCuotaData
            {
                CuotaId = cuota.CuotaId,
                NumeroCuota = cuota.NumeroCuota,
                CantidadCuotas = venta.CantidadCuotas ?? venta.Cuotas.Count,
                Cliente = venta.Cliente.Nombre,
                Vehiculo = $"{venta.Vehiculo.Modelo.Marca.Nombre} {venta.Vehiculo.Modelo.Nombre}",
                FormaPago = cuota.FormaPago?.Descripcion ?? "—",
                Observacion = cuota.ObservacionPago,
                FechaPago = cuota.FechaRegistroPago ?? cuota.FechaPago ?? DateTime.Now,
                SaldoInicial = saldoInicial,
                SaldoAnterior = saldoAnterior,
                Abono = abono,
                Recargo = recargo,
                SaldoActual = Math.Max(0, saldoActual),
                CI_RUC = cuota.Venta.Cliente.CI_RUC ?? "",
                UsuarioRegistro = venta.Vendedor?.UsuarioNombre ?? "Sistema"
            };

            var pdfBytes = TicketCuotaPdfService.Generar(data);
            var fileName = $"Ticket-Cuota{cuota.NumeroCuota}-Venta{venta.VentaId}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
