using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlayaAutos.API.Data;
using PlayaAutos.API.Models;
using System.Globalization;

namespace PlayaAutos.API.Services
{
    public class RecordatoriosOptions
    {
        public bool Habilitado { get; set; } = true;

        /// <summary>Con cuántos días de anticipación se avisa que una cuota está por vencer.</summary>
        public int DiasAntes { get; set; } = 3;

        /// <summary>Cada cuántos minutos se revisan las cuotas.</summary>
        public int IntervaloMinutos { get; set; } = 60;

        /// <summary>Solo se envían avisos entre estas horas (no despertar a nadie de madrugada).</summary>
        public int HoraDesde { get; set; } = 8;
        public int HoraHasta { get; set; } = 20;
    }

    /// <summary>
    /// Recordatorios de vencimiento de cuotas por notificación push:
    /// - "previo": la cuota vence dentro de DiasAntes días (por defecto 3);
    /// - "vence hoy": el mismo día del vencimiento.
    /// Cada aviso se envía una sola vez por cuota (Cuota.RecordatorioPrevioEnviado /
    /// RecordatorioVencimientoEnviado), aunque la API se reinicie. Solo cuotas
    /// Pendiente: las que están en revisión o pagadas no se recuerdan.
    /// </summary>
    public class RecordatoriosCuotaService
    {
        private static readonly CultureInfo Es = new("es-PY");

        private readonly AppDbContext _context;
        private readonly NotificacionesService _notificaciones;
        private readonly FirebasePushSender _push;
        private readonly RecordatoriosOptions _opciones;
        private readonly ILogger<RecordatoriosCuotaService> _logger;

        public RecordatoriosCuotaService(AppDbContext context, NotificacionesService notificaciones,
            FirebasePushSender push, IOptions<RecordatoriosOptions> opciones, ILogger<RecordatoriosCuotaService> logger)
        {
            _context = context;
            _notificaciones = notificaciones;
            _push = push;
            _opciones = opciones.Value;
            _logger = logger;
        }

        /// <summary>Envía los recordatorios pendientes. Devuelve cuántos se enviaron.</summary>
        public async Task<int> EnviarPendientesAsync(CancellationToken ct = default)
        {
            // Sin Firebase no se marca nada como enviado: cuando se configure,
            // los avisos que correspondan saldrán normalmente.
            if (!_push.Habilitado) return 0;

            var hoy = DateTime.Today;
            var limite = hoy.AddDays(Math.Max(1, _opciones.DiasAntes));

            var cuotas = await _context.Cuotas
                .Include(c => c.Venta).ThenInclude(v => v.Cliente)
                .Include(c => c.Venta).ThenInclude(v => v.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Where(c => c.Estado == EstadosCuota.Pendiente
                    && c.Venta.Cliente.UsuarioId != null
                    && c.FechaVencimiento >= hoy
                    && c.FechaVencimiento < limite.AddDays(1)
                    && ((c.FechaVencimiento < hoy.AddDays(1) && c.RecordatorioVencimientoEnviado == null)
                        || (c.FechaVencimiento >= hoy.AddDays(1) && c.RecordatorioPrevioEnviado == null)))
                .ToListAsync(ct);

            var enviados = 0;
            foreach (var cuota in cuotas)
            {
                var venceHoy = cuota.FechaVencimiento.Date == hoy;
                var (titulo, cuerpo) = Mensaje(cuota, venceHoy, hoy);

                await _notificaciones.NotificarUsuarioAsync(cuota.Venta.Cliente.UsuarioId,
                    titulo, cuerpo, "cuota", cuota.CuotaId);

                if (venceHoy) cuota.RecordatorioVencimientoEnviado = DateTime.Now;
                else cuota.RecordatorioPrevioEnviado = DateTime.Now;
                enviados++;
            }

            if (enviados > 0)
            {
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Recordatorios de vencimiento enviados: {Cantidad}.", enviados);
            }
            return enviados;
        }

        private static (string Titulo, string Cuerpo) Mensaje(Cuota cuota, bool venceHoy, DateTime hoy)
        {
            var v = cuota.Venta.Vehiculo;
            var vehiculo = v?.Modelo != null ? $" ({v.Modelo.Marca.Nombre} {v.Modelo.Nombre})" : "";
            var de = cuota.Venta.CantidadCuotas.HasValue ? $" de {cuota.Venta.CantidadCuotas}" : "";
            var monto = "₲ " + cuota.Monto.ToString("N0", Es);
            var descripcion = $"cuota {cuota.NumeroCuota}{de}{vehiculo}";

            if (venceHoy)
                return ("Tu cuota vence hoy",
                    $"Hoy vence tu {descripcion} por {monto}. Pagala desde la app para evitar recargos.");

            var dias = (cuota.FechaVencimiento.Date - hoy).Days;
            var cuando = dias == 1 ? "mañana" : $"en {dias} días, el {cuota.FechaVencimiento:dd/MM}";
            return ("Cuota próxima a vencer",
                $"Tu {descripcion} por {monto} vence {cuando}. Podés pagarla desde la app.");
        }
    }

    /// <summary>
    /// Ejecuta RecordatoriosCuotaService al iniciar la API y luego cada
    /// IntervaloMinutos, solo dentro del horario permitido.
    /// </summary>
    public class RecordatoriosCuotaWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly RecordatoriosOptions _opciones;
        private readonly ILogger<RecordatoriosCuotaWorker> _logger;

        public RecordatoriosCuotaWorker(IServiceScopeFactory scopes, IOptions<RecordatoriosOptions> opciones,
            ILogger<RecordatoriosCuotaWorker> logger)
        {
            _scopes = scopes;
            _opciones = opciones.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_opciones.Habilitado) return;

            // Pequeña espera para no competir con el arranque de la API.
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { return; }

            var intervalo = TimeSpan.FromMinutes(Math.Max(5, _opciones.IntervaloMinutos));
            while (!stoppingToken.IsCancellationRequested)
            {
                var hora = DateTime.Now.Hour;
                if (hora >= _opciones.HoraDesde && hora < _opciones.HoraHasta)
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var servicio = scope.ServiceProvider.GetRequiredService<RecordatoriosCuotaService>();
                        await servicio.EnviarPendientesAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) { return; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar los recordatorios de vencimiento de cuotas.");
                    }
                }

                try { await Task.Delay(intervalo, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
