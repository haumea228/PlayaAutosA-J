using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class FacturasController : AdminVendedorController
    {
        private readonly ApiService _api;

        public FacturasController(ApiService api)
        {
            _api = api;
        }

        // ── GET /Facturas ────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var facturas = await _api.GetAsync<List<FacturaListItem>>("api/Facturas")
                           ?? new List<FacturaListItem>();
            return View(facturas);
        }

        // ── GET /Facturas/Detalle/{id} ────────────────────────────────────
        public async Task<IActionResult> Detalle(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var dto = await _api.GetAsync<JsonElement>($"api/Facturas/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            var factura = new FacturaViewModel
            {
                FacturaId = id,
                NumeroFactura = Get(dto, "numeroFactura"),
                Timbrado = Get(dto, "timbrado"),
                FechaEmision = DateTime.TryParse(Get(dto, "fechaEmision"), out var f) ? f : DateTime.Now,
                Cliente = Get(dto, "cliente"),
                Vehiculo = Get(dto, "vehiculo"),
                Subtotal = long.TryParse(Get(dto, "subtotal"), out var s) ? s : 0,
                IVA = long.TryParse(Get(dto, "iva"), out var i) ? i : 0,
                Total = long.TryParse(Get(dto, "total"), out var t) ? t : 0,
                RutaPDF = Get(dto, "rutaPDF")
            };

            return View(factura);
        }

        // ── GET /Facturas/DescargarPDF/{id} ─────────────────────────────
        public async Task<IActionResult> DescargarPDF(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var response = await _api.GetRawAsync($"api/Facturas/{id}/pdf");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo generar el PDF de la factura.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();

            var nombreArchivo = response.Content.Headers.ContentDisposition?.FileNameStar
                             ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                             ?? $"Factura-{id}.pdf";

            return File(bytes, "application/pdf", nombreArchivo);
        }

        // ── Helpers ──────────────────────────────────────────────────────
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
