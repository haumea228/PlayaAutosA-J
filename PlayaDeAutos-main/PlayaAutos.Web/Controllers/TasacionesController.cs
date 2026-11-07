using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class TasacionesController : AdminVendedorController
    {
        private readonly ApiService _api;

        public TasacionesController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(string? filtro)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var endpoint = filtro == "pendientes" ? "api/Tasaciones/pendientes" : "api/Tasaciones";
            var tasaciones = await _api.GetAsync<List<TasacionListItem>>(endpoint) ?? new();
            if (!string.IsNullOrEmpty(filtro) && filtro != "pendientes")
                tasaciones = tasaciones.Where(t => t.EstadoTasacion.Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.FiltroActual = filtro ?? "todas";
            return View(tasaciones);
        }

        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            var vm = new TasacionViewModel { AnhoVehiculo = DateTime.Now.Year };
            await CargarDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(TasacionViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) { await CargarDropdowns(vm); return View(vm); }

            var resp = await _api.PostAsync("api/Tasaciones", new
            {
                vm.ClienteId,
                vm.MarcaVehiculo,
                vm.ModeloVehiculo,
                vm.AnhoVehiculo,
                vm.KilometrajeVehiculo,
                vm.EstadoGeneralVehiculo,
                vm.ColorVehiculo,
                vm.ValorTasacion,
                vm.PrecioVenta,
                vm.ModeloId,
                vm.TipoId,
                vm.CondicionId,
                vm.OrigenId,
                vm.VehiculoId  // 🟢 NUEVO
            });

            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Error al registrar la tasación.");
                await CargarDropdowns(vm); return View(vm);
            }
            TempData["Exito"] = "Tasación registrada.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Aprobar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            var tas = await _api.GetAsync<JsonElement>($"api/Tasaciones/{id}");
            if (tas.ValueKind == JsonValueKind.Undefined) return NotFound();
            if (Get(tas, "estadoTasacion") != "Pendiente")
            {
                TempData["Error"] = "Solo se pueden aprobar tasaciones Pendientes.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new AprobarTasacionViewModel
            {
                TasacionVehiculoId = id,
                ClienteNombre = Get(tas, "cliente"),
                MarcaVehiculo = Get(tas, "marcaVehiculo"),
                ModeloVehiculo = Get(tas, "modeloVehiculo"),
                AnhoVehiculo = int.TryParse(Get(tas, "anhoVehiculo"), out var a) ? a : 0,
                KilometrajeVehiculo = long.TryParse(Get(tas, "kilometrajeVehiculo"), out var km) ? km : 0,
                EstadoGeneralVehiculo = Get(tas, "estadoGeneralVehiculo"),
                ColorVehiculo = Get(tas, "colorVehiculo"),
                ValorTasacionFinal = long.TryParse(Get(tas, "valorTasacion"), out var vt) ? vt : 0,
                PrecioVentaFinal = long.TryParse(Get(tas, "precioVenta"), out var pv) ? pv : 0,
                Color = Get(tas, "colorVehiculo"),
                Kilometraje = long.TryParse(Get(tas, "kilometrajeVehiculo"), out var km2) ? km2 : 0,
                ModeloId = int.TryParse(Get(tas, "modeloId"), out var moid) ? moid : 0,
                TipoId = int.TryParse(Get(tas, "tipoId"), out var tid) ? tid : 0,
                CondicionId = int.TryParse(Get(tas, "condicionId"), out var cid) ? cid : 0,
                OrigenId = int.TryParse(Get(tas, "origenId"), out var oid) ? oid : 0
            };

            await CargarDropdownsAprobar(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id, AprobarTasacionViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) { await CargarDropdownsAprobar(vm); return View(vm); }
            var resp = await _api.PutAsync($"api/Tasaciones/{id}/aprobar", new { vm.ValorTasacionFinal, vm.PrecioVentaFinal, vm.ModeloId, vm.TipoId, vm.CondicionId, vm.OrigenId, vm.Color, vm.Kilometraje });
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode ? "Tasación aprobada." : "Error al aprobar.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            var resp = await _api.PutAsync($"api/Tasaciones/{id}/rechazar", new { });
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode ? "Tasación rechazada." : "Error al rechazar.";
            return RedirectToAction(nameof(Index));
        }

        private bool EstaAutenticado() => HttpContext.Session.GetString("Token") != null;

        private async Task CargarDropdowns(TasacionViewModel vm)
        {
            var clientes = await _api.GetAsync<List<JsonElement>>("api/Clientes") ?? new();
            vm.Clientes = clientes.Where(c => c.TryGetProperty("activo", out var a) && a.GetBoolean())
                .Select(c => new SelectListItem(Get(c, "nombre") + " — " + Get(c, "ci_RUC"), Get(c, "clienteId"))).ToList();

            var modelosRaw = await _api.GetAsync<List<JsonElement>>("api/Modelos") ?? new();
            vm.ModelosConMarca = modelosRaw.Select(m => new ModeloSelectItem { ModeloId = int.TryParse(Get(m, "modeloId"), out var mid) ? mid : 0, NombreModelo = Get(m, "nombre"), NombreMarca = Get(m, "marca") }).Where(m => m.ModeloId > 0).OrderBy(m => m.NombreMarca).ThenBy(m => m.NombreModelo).ToList();

            var tipos = await _api.GetAsync<List<JsonElement>>("api/TiposVehiculo") ?? new();
            vm.Tipos = tipos.Select(t => new SelectListItem(Get(t, "descripcion"), Get(t, "tipoId"))).ToList();

            var condiciones = await _api.GetAsync<List<JsonElement>>("api/CondicionesVehiculo") ?? new();
            vm.Condiciones = condiciones.Select(c => new SelectListItem(Get(c, "descripcion"), Get(c, "condicionId"))).ToList();

            var origenes = await _api.GetAsync<List<JsonElement>>("api/OrigenesVehiculo") ?? new();
            vm.Origenes = origenes.Select(o => new SelectListItem(Get(o, "descripcion"), Get(o, "origenId"))).ToList();

            // 🟢 Vehículos del catálogo
            var vehiculosCat = await _api.GetAsync<List<JsonElement>>("api/Vehiculos") ?? new();
            vm.VehiculosCatalogo = vehiculosCat.Select(v => new SelectListItem(
                $"{Get(v, "codigoInterno")} — {Get(v, "marca")} {Get(v, "modelo")} ({Get(v, "anio")}) - {Get(v, "estado")}",
                Get(v, "vehiculoId"))).ToList();
        }

        private async Task CargarDropdownsAprobar(AprobarTasacionViewModel vm)
        {
            var modelosRaw = await _api.GetAsync<List<JsonElement>>("api/Modelos") ?? new();
            vm.ModelosConMarca = modelosRaw.Select(m => new ModeloSelectItem { ModeloId = int.TryParse(Get(m, "modeloId"), out var mid) ? mid : 0, NombreModelo = Get(m, "nombre"), NombreMarca = Get(m, "marca") }).Where(m => m.ModeloId > 0).OrderBy(m => m.NombreMarca).ThenBy(m => m.NombreModelo).ToList();
            var tipos = await _api.GetAsync<List<JsonElement>>("api/TiposVehiculo") ?? new();
            vm.Tipos = tipos.Select(t => new SelectListItem(Get(t, "descripcion"), Get(t, "tipoId"))).ToList();
            var condiciones = await _api.GetAsync<List<JsonElement>>("api/CondicionesVehiculo") ?? new();
            vm.Condiciones = condiciones.Select(c => new SelectListItem(Get(c, "descripcion"), Get(c, "condicionId"))).ToList();
            var origenes = await _api.GetAsync<List<JsonElement>>("api/OrigenesVehiculo") ?? new();
            vm.Origenes = origenes.Select(o => new SelectListItem(Get(o, "descripcion"), Get(o, "origenId"))).ToList();
        }

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val)) return val.ValueKind == JsonValueKind.Number ? val.GetRawText() : val.GetString() ?? "";
            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var val2)) return val2.ValueKind == JsonValueKind.Number ? val2.GetRawText() : val2.GetString() ?? "";
            return "";
        }

        [HttpGet]
        [Route("/Tasaciones/GetVehiculo/{id}")]
        public async Task<IActionResult> GetVehiculo(int id)
        {
            var vehiculo = await _api.GetAsync<JsonElement>($"api/Vehiculos/{id}");
            return Json(vehiculo);
        }
    }
}