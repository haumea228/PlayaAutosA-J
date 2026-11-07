using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayaAutos.API.Models;
using PlayaAutos.API.Services;

namespace PlayaAutos.API.Controllers
{
    /// <summary>
    /// Páginas a las que Stripe redirige el navegador del cliente al terminar o
    /// cancelar el pago. Son públicas porque las abre el navegador (sin JWT):
    /// solo muestran un mensaje; el cobro siempre se verifica contra Stripe.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PagosOnlineController : ControllerBase
    {
        private readonly PagoOnlineService _pagosOnline;
        private readonly ILogger<PagosOnlineController> _logger;

        public PagosOnlineController(PagoOnlineService pagosOnline, ILogger<PagosOnlineController> logger)
        {
            _pagosOnline = pagosOnline;
            _logger = logger;
        }

        // GET: api/pagosonline/retorno?session_id=cs_test_...
        [HttpGet("retorno")]
        public async Task<ContentResult> Retorno([FromQuery(Name = "session_id")] string? sesionId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sesionId) || sesionId.Length > 255)
                return Pagina("Pago", "No se encontró el pago.", "Volvé a la app para ver el estado de tu cuota.", false);

            try
            {
                var pago = await _pagosOnline.ConfirmarPorSesionAsync(sesionId, ct);
                return pago?.Estado switch
                {
                    EstadosPagoOnline.Pagado => Pagina("Pago confirmado", "¡Listo! Recibimos tu pago.",
                        "Tu cuota ya figura como pagada. Volvé a la app de A&J Autos para ver el ticket.", true),
                    EstadosPagoOnline.Pendiente => Pagina("Procesando pago", "Estamos confirmando tu pago.",
                        "Volvé a la app de A&J Autos: en unos segundos vas a ver la cuota actualizada.", true),
                    _ => Pagina("Pago", "No pudimos confirmar el pago.",
                        "Volvé a la app de A&J Autos para revisar el estado de tu cuota.", false)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al confirmar el pago online de la sesión {Sesion}.", sesionId);
                return Pagina("Procesando pago", "Estamos confirmando tu pago.",
                    "Volvé a la app de A&J Autos: la cuota se va a actualizar automáticamente.", true);
            }
        }

        // GET: api/pagosonline/cancelado
        [HttpGet("cancelado")]
        public ContentResult Cancelado() =>
            Pagina("Pago cancelado", "No se realizó ningún cobro.",
                "Podés volver a la app de A&J Autos y elegir otra forma de pago.", false);

        private static ContentResult Pagina(string titulo, string encabezado, string texto, bool exito)
        {
            var color = exito ? "#16a34a" : "#9A7C65";
            var icono = exito ? "&#10004;" : "&#8505;";
            var html = $$"""
                <!doctype html>
                <html lang="es">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>{{WebUtility.HtmlEncode(titulo)}} — A&amp;J Autos</title>
                  <style>
                    body { margin:0; min-height:100vh; display:flex; align-items:center; justify-content:center;
                           font-family: system-ui, -apple-system, Roboto, sans-serif; background:#FAF6F3; color:#0C2E3D; }
                    .card { max-width:420px; margin:24px; padding:32px 24px; background:#fff; border-radius:22px;
                            box-shadow:0 4px 24px rgba(12,46,61,.12); text-align:center; }
                    .icono { width:64px; height:64px; margin:0 auto 16px; border-radius:50%; display:flex;
                             align-items:center; justify-content:center; font-size:30px; color:#fff; background:{{color}}; }
                    h1 { font-size:1.35rem; margin:0 0 8px; }
                    p { margin:0; line-height:1.5; color:#475569; }
                  </style>
                </head>
                <body>
                  <div class="card">
                    <div class="icono">{{icono}}</div>
                    <h1>{{WebUtility.HtmlEncode(encabezado)}}</h1>
                    <p>{{WebUtility.HtmlEncode(texto)}}</p>
                  </div>
                </body>
                </html>
                """;
            return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 200 };
        }
    }
}
