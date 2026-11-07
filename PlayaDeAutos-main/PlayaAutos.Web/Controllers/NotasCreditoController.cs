using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class NotasCreditoController : AdminVendedorController
    {
        private readonly ApiService _api;

        public NotasCreditoController(ApiService api)
        {
            _api = api;
        }

        // GET /NotasCredito
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var notas = await _api.GetAsync<List<NotaCreditoListItem>>("api/NotasCredito")
                        ?? new List<NotaCreditoListItem>();
            return View(notas);
        }

        // GET /NotasCredito/Detalle/{id}
        public async Task<IActionResult> Detalle(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var dto = await _api.GetAsync<JsonElement>($"api/NotasCredito/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            var vm = new NotaCreditoViewModel
            {
                NotaCreditoId    = id,
                VentaId          = int.TryParse(Get(dto, "ventaId"), out var vid) ? vid : 0,
                NumeroNota       = Get(dto, "numeroNota"),
                Timbrado         = Get(dto, "timbrado"),
                FechaEmision     = DateTime.TryParse(Get(dto, "fechaEmision"), out var fe) ? fe : DateTime.Now,
                Cliente          = Get(dto, "cliente"),
                ClienteRUC       = Get(dto, "clienteRUC"),
                ClienteDireccion = Get(dto, "clienteDireccion"),
                ClienteTelefono  = Get(dto, "clienteTelefono"),
                VehiculoVenta    = Get(dto, "vehiculoVenta"),
                VehiculoPermuta  = Get(dto, "vehiculoPermuta") is { Length: > 0 } vp ? vp : null,
                Motivo           = Get(dto, "motivo"),
                Monto            = long.TryParse(Get(dto, "monto"), out var m) ? m : 0,
                NumeroFactura    = Get(dto, "numeroFactura")
            };

            return View(vm);
        }

        // GET /NotasCredito/DescargarPDF/{id}
        public async Task<IActionResult> DescargarPDF(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var response = await _api.GetRawAsync($"api/NotasCredito/{id}/pdf");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo generar el PDF de la nota de crédito.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var nombreArchivo = response.Content.Headers.ContentDisposition?.FileNameStar
                             ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                             ?? $"NotaCredito-{id}.pdf";

            return File(bytes, "application/pdf", nombreArchivo);
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val))
                return val.ValueKind == JsonValueKind.Number
                    ? val.GetRawText()
                    : val.GetString() ?? "";

            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var val2))
                return val2.ValueKind == JsonValueKind.Number
                    ? val2.GetRawText()
                    : val2.GetString() ?? "";

            return "";
        }
    }
}