using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class GastosController : AdminVendedorController
    {
        private readonly ApiService _api;

        public GastosController(ApiService api)
        {
            _api = api;
        }

        // ── GET /Gastos ──────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var gastos = await _api.GetAsync<List<GastoListItem>>("api/Gastos")
                         ?? new List<GastoListItem>();
            return View(gastos);
        }

        // ── GET /Gastos/Crear ────────────────────────────────────────────
        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vm = new CrearGastoViewModel { Fecha = DateTime.Today };
            await CargarDropdowns(vm);
            return View(vm);
        }

        // ── POST /Gastos/Crear ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearGastoViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(vm);
                return View(vm);
            }

            var vendedorId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            var resp = await _api.PostAsync("api/Gastos", new
            {
                vm.VehiculoId,
                vm.TipoGastoId,
                vm.Descripcion,
                vm.Monto,
                vm.Proveedor,
                vm.Fecha,
                UsuarioRegistro = vendedorId
            });

            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Gasto registrado correctamente. Movimiento de caja generado automáticamente."
                : "Error al registrar el gasto.";

            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private async Task CargarDropdowns(CrearGastoViewModel vm)
        {
            var vehiculos = await _api.GetAsync<List<JsonElement>>("api/Vehiculos") ?? new();
            vm.Vehiculos = vehiculos
                .Select(v => new SelectListItem(
                    $"{Get(v, "codigoInterno")} — {Get(v, "marca")} {Get(v, "modelo")}",
                    Get(v, "vehiculoId")))
                .ToList();

            var tipos = await _api.GetAsync<List<JsonElement>>("api/Gastos/tipos") ?? new();
            vm.TiposGasto = tipos
                .Select(t => new SelectListItem(
                    Get(t, "descripcion"),
                    Get(t, "tipoGastoId")))
                .ToList();
        }

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