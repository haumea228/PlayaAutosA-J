using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class CitasController : AdminVendedorController
    {
        private readonly ApiService _api;

        public CitasController(ApiService api)
        {
            _api = api;
        }

        // ── GET /Citas ──────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var citas = await _api.GetAsync<List<CitaListItem>>("api/Citas")
                       ?? new List<CitaListItem>();
            return View(citas);
        }

        // ── GET /Citas/Crear ────────────────────────────────────────────
        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            // Próxima hora entera a partir de ahora (mínimo 1 hora en el futuro)
            var ahora = DateTime.Now;
            var proximaHora = ahora.AddHours(1);
            var defaultFecha = new DateTime(proximaHora.Year, proximaHora.Month, proximaHora.Day,
                                            proximaHora.Hour, 0, 0);

            var vm = new CitaViewModel
            {
                FechaHora = defaultFecha,
                TipoCita = "Visita"
            };
            await CargarDropdowns(vm);
            return View(vm);
        }

        // ── POST /Citas/Crear ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CitaViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.FechaHora < DateTime.Now.AddMinutes(-15))
                ModelState.AddModelError("FechaHora", "La fecha y hora no puede ser en el pasado.");

            // EstadoCitaId no viene del form — siempre es 1 (Pendiente)
            ModelState.Remove(nameof(vm.EstadoCitaId));
            vm.EstadoCitaId = 1;

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(vm);
                return View(vm);
            }

            var response = await _api.PostAsync("api/Citas", new
            {
                vm.ClienteId,
                vm.VehiculoId,
                vm.VendedorId,
                EstadoCitaId = 1,
                vm.FechaHora,
                vm.TipoCita,
                vm.Observaciones
            });

            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", "Error al registrar la cita. Intente nuevamente.");
                await CargarDropdowns(vm);
                return View(vm);
            }

            TempData["Exito"] = "Cita registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Citas/Editar/{id} ──────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var dto = await _api.GetAsync<JsonElement>($"api/Citas/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            var vm = new CitaViewModel
            {
                CitaId = id,
                ClienteId = int.Parse(Get(dto, "clienteId")),
                VehiculoId = int.TryParse(Get(dto, "vehiculoId"), out var vid) ? vid : null,
                VendedorId = int.Parse(Get(dto, "vendedorId")),
                EstadoCitaId = int.Parse(Get(dto, "estadoCitaId")),
                FechaHora = DateTime.TryParse(Get(dto, "fechaHora"), out var fh) ? fh : DateTime.Now,
                TipoCita = Get(dto, "tipoCita"),
                Observaciones = Get(dto, "observaciones"),
                NombreCliente = Get(dto, "cliente"),
                VehiculoInfo = Get(dto, "vehiculo"),
                VendedorNombre = Get(dto, "vendedor"),
                EstadoDescripcion = Get(dto, "estado"),
                EstadoColor = Get(dto, "estadoColor")
            };

            await CargarDropdowns(vm);
            return View(vm);
        }

        // ── POST /Citas/Editar/{id} ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CitaViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.FechaHora < DateTime.Now.AddHours(-1))
                ModelState.AddModelError("FechaHora", "La fecha no puede ser muy antigua.");

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(vm);
                return View(vm);
            }

            var resp = await _api.PutAsync($"api/Citas/{id}", new
            {
                vm.ClienteId,
                vm.VehiculoId,
                vm.VendedorId,
                vm.FechaHora,
                vm.TipoCita,
                vm.Observaciones
            });

            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Error al actualizar la cita.");
                await CargarDropdowns(vm);
                return View(vm);
            }

            TempData["Exito"] = "Cita actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Citas/CambiarEstado ───────────────────────────────────
        // ✅ CORREGIDO: [HttpPost] y [ValidateAntiForgeryToken] explícitos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, int estadoCitaId)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var resp = await _api.PutAsync($"api/Citas/{id}/estado", new { estadoCitaId });

            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Estado de la cita actualizado."
                : "Error al cambiar el estado.";

            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private async Task CargarDropdowns(CitaViewModel vm)
        {
            var clientes = await _api.GetAsync<List<JsonElement>>("api/Clientes") ?? new();
            vm.Clientes = clientes
                .Where(c => c.TryGetProperty("activo", out var a) && a.GetBoolean())
                .Select(c => new SelectListItem(
                    $"{Get(c, "nombre")} — {Get(c, "ci_RUC")}",
                    Get(c, "clienteId")))
                .ToList();

            var vehiculos = await _api.GetAsync<List<JsonElement>>("api/Vehiculos") ?? new();
            vm.Vehiculos = vehiculos
                .Where(v =>
                {
                    if (!v.TryGetProperty("estado", out var est)) return false;
                    var estadoStr = est.ValueKind == JsonValueKind.String
                        ? est.GetString() ?? ""
                        : "";
                    return estadoStr.Equals("Disponible", StringComparison.OrdinalIgnoreCase)
                           || estadoStr.Equals("Reservado", StringComparison.OrdinalIgnoreCase);
                })
                .Select(v => new SelectListItem(
                    $"{Get(v, "codigoInterno")} — {Get(v, "marca")} {Get(v, "modelo")} ({Get(v, "anio")})",
                    Get(v, "vehiculoId")))
                .ToList();

            var usuarios = await _api.GetAsync<List<JsonElement>>("api/Usuarios") ?? new();
            vm.Vendedores = usuarios
                .Where(u =>
                {
                    var rol = Get(u, "rol");
                    return rol.Contains("Vendedor") || rol.Contains("Administrador");
                })
                .Select(u => new SelectListItem(
                    Get(u, "usuarioNombre"),
                    Get(u, "usuarioId")))
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