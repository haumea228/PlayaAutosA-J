using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class UsuariosController : AdminVendedorController
    {
        private readonly ApiService _api;

        public UsuariosController(ApiService api)
        {
            _api = api;
        }

        // ── GET /Usuarios ────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            var usuarios = await _api.GetAsync<List<UsuarioListItem>>("api/Usuarios")
                           ?? new List<UsuarioListItem>();
            return View(usuarios);
        }

        // ── GET /Usuarios/Crear ──────────────────────────────────────────
        public IActionResult Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            return View(new UsuarioViewModel());
        }

        // ── POST /Usuarios/Crear ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError("Password", "La contraseña es obligatoria al crear un usuario.");

            if (!ModelState.IsValid)
                return View(vm);

            var response = await _api.PostAsync("api/Usuarios", new
            {
                vm.UsuarioNombre,
                vm.UsuarioEmail,
                vm.UsuarioPhone,
                vm.Rol,
                Password = vm.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("email") || detalle.Contains("Email")
                    ? "Ya existe un usuario registrado con ese email."
                    : "Error al crear el usuario. Intente nuevamente.");
                return View(vm);
            }

            TempData["Exito"] = $"Usuario {vm.UsuarioNombre} creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Usuarios/Editar/{id} ────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            var dto = await _api.GetAsync<JsonElement>($"api/Usuarios/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            var vm = new UsuarioViewModel
            {
                UsuarioId = id,
                UsuarioNombre = Get(dto, "usuarioNombre"),
                UsuarioEmail = Get(dto, "usuarioEmail"),
                UsuarioPhone = Get(dto, "usuarioPhone"),
                Rol = Get(dto, "rol"),
                ActivoUsuario = dto.TryGetProperty("activoUsuario", out var activo) && activo.GetBoolean()
            };

            return View(vm);
        }

        // ── POST /Usuarios/Editar/{id} ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, UsuarioViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            // La contraseña es opcional al editar
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                vm.UsuarioId = id;
                return View(vm);
            }

            var resp = await _api.PutAsync($"api/Usuarios/{id}", new
            {
                vm.UsuarioNombre,
                vm.UsuarioEmail,
                vm.UsuarioPhone,
                vm.Rol,
                vm.ActivoUsuario,
                NuevaPassword = string.IsNullOrWhiteSpace(vm.NuevaPassword) ? null : vm.NuevaPassword
            });

            if (!resp.IsSuccessStatusCode)
            {
                var detalle = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("email") || detalle.Contains("Email")
                    ? "Ya existe un usuario con ese email."
                    : "Error al actualizar el usuario. Intente nuevamente.");
                vm.UsuarioId = id;
                return View(vm);
            }

            TempData["Exito"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Usuarios/ToggleActivo ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            // No permitir desactivarse a sí mismo
            if (HttpContext.Session.GetInt32("UsuarioId") == id)
            {
                TempData["Error"] = "No puede desactivar su propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            var resp = await _api.PutAsync($"api/Usuarios/{id}/toggle-activo", new { });
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Estado del usuario actualizado."
                : "No se pudo cambiar el estado del usuario.";

            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private bool EsAdministrador() =>
            HttpContext.Session.GetString("Rol") == "AdministradorP";

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val))
                return val.ValueKind == JsonValueKind.Number
                    ? val.GetRawText()
                    : val.GetString() ?? "";
            var lower = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(lower, out var val2))
                return val2.ValueKind == JsonValueKind.Number
                    ? val2.GetRawText()
                    : val2.GetString() ?? "";
            return "";
        }
    }
}