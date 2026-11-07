using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class ConsignasController : AdminVendedorController
    {
        private readonly ApiService _api;

        public ConsignasController(ApiService api)
        {
            _api = api;
        }

        // GET /Consignas
        public async Task<IActionResult> Index(string? filtro)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var lista = await _api.GetAsync<List<ConsignaListItem>>("api/Consignas")
                        ?? new List<ConsignaListItem>();

            filtro = filtro?.ToLower() ?? "todas";
            lista = filtro switch
            {
                "vigente"  => lista.Where(c => c.Estado == "Vigente").ToList(),
                "vencido"  => lista.Where(c => c.Estado == "Vencido").ToList(),
                "anulado"  => lista.Where(c => c.Estado == "Anulado").ToList(),
                _          => lista
            };

            ViewBag.FiltroActual = filtro;
            return View(lista);
        }

        // GET /Consignas/Crear
        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vm = new ConsignaViewModel();
            await CargarConsignantes(vm);
            return View(vm);
        }

        // POST /Consignas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ConsignaViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.FechaFin <= vm.FechaInicio)
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio.");

            if (!ModelState.IsValid)
            {
                await CargarConsignantes(vm);
                return View(vm);
            }

            var resp = await _api.PostAsync("api/Consignas", new
            {
                vm.ConsignanteId,
                vm.TasacionVehiculoId,
                vm.FechaInicio,
                vm.FechaFin,
                vm.PorcentajeComision,
                vm.Clausulas
            });

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", string.IsNullOrWhiteSpace(err)
                    ? "Error al registrar la consigna. Verificá los datos."
                    : err.Trim('"'));
                await CargarConsignantes(vm);
                return View(vm);
            }

            var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var contratoId = result.GetProperty("contratoId").GetInt32();

            TempData["Exito"] = "Consigna registrada correctamente.";
            return RedirectToAction(nameof(Contrato), new { id = contratoId });
        }

        // POST /Consignas/Anular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var resp = await _api.PutAsync($"api/Consignas/{id}/anular", new { });

            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Consigna anulada. El vehículo fue retirado del catálogo."
                : "No se pudo anular la consigna. Intente nuevamente.";

            return RedirectToAction(nameof(Index));
        }

        // GET /Consignas/Contrato/{id}
        public async Task<IActionResult> Contrato(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vm = await _api.GetAsync<ConsignaContratoViewModel>($"api/Consignas/{id}/contrato");
            if (vm == null) return NotFound();
            return View(vm);
        }

        // GET /Consignas/TasacionesPorConsignante/{id}  (AJAX)
        [HttpGet]
        public async Task<IActionResult> TasacionesPorConsignante(int id)
        {
            var tasaciones = await _api.GetAsync<List<JsonElement>>($"api/Consignas/tasaciones/{id}");
            return Json(tasaciones ?? new List<JsonElement>());
        }

        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private async Task CargarConsignantes(ConsignaViewModel vm)
        {
            var consignantes = await _api.GetAsync<List<JsonElement>>("api/Consignas/consignantes") ?? new();
            vm.Consignantes = consignantes.Select(c => new SelectListItem(
                Get(c, "nombre") + (string.IsNullOrEmpty(Get(c, "ci_RUC")) ? "" : " — " + Get(c, "ci_RUC")),
                Get(c, "consignanteId")
            )).ToList();
        }

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val))
                return val.ValueKind == JsonValueKind.Number ? val.GetRawText() : val.GetString() ?? "";
            var lower = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(lower, out var val2))
                return val2.ValueKind == JsonValueKind.Number ? val2.GetRawText() : val2.GetString() ?? "";
            return "";
        }
    }
}
