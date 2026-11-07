using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class ClientesController : AdminVendedorController
    {
        private readonly ApiService _api;

        public ClientesController(ApiService api)
        {
            _api = api;
        }

        // ── GET /Clientes ────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var clientes = await _api.GetAsync<List<ClienteListItem>>("api/Clientes")
                           ?? new List<ClienteListItem>();
            return View(clientes);
        }

        // ── GET /Clientes/Crear ──────────────────────────────────────────
        public IActionResult Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            return View(new ClienteViewModel());
        }

        // ── POST /Clientes/Crear ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClienteViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            // Validación cruzada de rango de precios
            if (vm.RangoPrecioMin.HasValue && vm.RangoPrecioMax.HasValue
                && vm.RangoPrecioMin > vm.RangoPrecioMax)
            {
                ModelState.AddModelError("RangoPrecioMax",
                    "El precio máximo debe ser mayor o igual al precio mínimo.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var response = await _api.PostAsync("api/Clientes", new
            {
                vm.Nombre,
                vm.CI_RUC,
                vm.Telefono,
                vm.Email,
                vm.Direccion,
                vm.RangoPrecioMin,
                vm.RangoPrecioMax
            });

            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("CI_RUC") || detalle.Contains("ci_ruc")
                    ? "Ya existe un cliente registrado con ese CI/RUC."
                    : "Error al registrar el cliente. Intente nuevamente.");
                return View(vm);
            }

            TempData["Exito"] = "Cliente registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Clientes/Editar/{id} ────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var dto = await _api.GetAsync<JsonElement>($"api/Clientes/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            var vm = new ClienteViewModel
            {
                ClienteId = id,
                Nombre = Get(dto, "nombre"),
                CI_RUC = Get(dto, "CI_RUC") ?? Get(dto, "ci_RUC") ?? Get(dto, "ci_ruc") ?? Get(dto, "Ci_Ruc") ?? "",
                Telefono = Get(dto, "telefono"),
                Email = Get(dto, "email"),
                Direccion = Get(dto, "direccion"),
                Activo = dto.TryGetProperty("activo", out var act) && act.GetBoolean(),
                RangoPrecioMin = dto.TryGetProperty("rangoPrecioMin", out var rMin)
                                 && rMin.ValueKind == JsonValueKind.Number
                                 ? rMin.GetInt64() : null,
                RangoPrecioMax = dto.TryGetProperty("rangoPrecioMax", out var rMax)
                                 && rMax.ValueKind == JsonValueKind.Number
                                 ? rMax.GetInt64() : null,
                EsConsignante = dto.TryGetProperty("esConsignante", out var esC) && esC.GetBoolean(),
                ConsignanteId = dto.TryGetProperty("consignanteId", out var cid)
                                && cid.ValueKind == JsonValueKind.Number
                                ? cid.GetInt32() : null,
                PorcentajeComisionDefault = dto.TryGetProperty("porcentajeComisionDefault", out var pct)
                                           && pct.ValueKind == JsonValueKind.Number
                                           ? pct.GetDecimal() : null,
            };

            return View(vm);
        }

        // ── POST /Clientes/Editar/{id} ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ClienteViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.RangoPrecioMin.HasValue && vm.RangoPrecioMax.HasValue
                && vm.RangoPrecioMin > vm.RangoPrecioMax)
            {
                ModelState.AddModelError("RangoPrecioMax",
                    "El precio máximo debe ser mayor o igual al precio mínimo.");
            }

            if (!ModelState.IsValid)
            {
                vm.ClienteId = id;
                return View(vm);
            }

            var resp = await _api.PutAsync($"api/Clientes/{id}", new
            {
                vm.Nombre,
                vm.CI_RUC,
                vm.Telefono,
                vm.Email,
                vm.Direccion,
                vm.Activo,
                vm.RangoPrecioMin,
                vm.RangoPrecioMax
            });

            if (!resp.IsSuccessStatusCode)
            {
                var detalle = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("CI_RUC") || detalle.Contains("ci_ruc")
                    ? "Ya existe un cliente registrado con ese CI/RUC."
                    : "Error al actualizar el cliente. Intente nuevamente.");
                vm.ClienteId = id;
                return View(vm);
            }

            TempData["Exito"] = "Cliente actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Clientes/Eliminar ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var resp = await _api.DeleteAsync($"api/Clientes/{id}");
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Cliente eliminado correctamente."
                : "No se pudo eliminar el cliente. Puede tener ventas asociadas.";

            return RedirectToAction(nameof(Index));
        }
        // ── POST /Clientes/ConvertirConsignante ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertirConsignante(int id, decimal porcentajeComisionDefault)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var resp = await _api.PutAsync($"api/Clientes/{id}/convertir-consignante", new
            {
                porcentajeComisionDefault
            });

            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Cliente habilitado como consignante correctamente."
                : "No se pudo convertir el cliente. Intente nuevamente.";

            return RedirectToAction(nameof(Editar), new { id });
        }

        // ── POST /Clientes/Reactivar ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            if (!EstaAutenticado())
                return Json(new { success = false, message = "No autenticado" });

            var resp = await _api.PutAsync<object>($"api/Clientes/{id}/reactivar", null);

            if (resp.IsSuccessStatusCode)
                return Json(new { success = true });

            return Json(new { success = false, message = "Error al reactivar" });
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
            // Fallback camelCase
            var lower = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(lower, out var val2))
                return val2.ValueKind == JsonValueKind.Number
                    ? val2.GetRawText()
                    : val2.GetString() ?? "";
            return "";
        }
    }
}