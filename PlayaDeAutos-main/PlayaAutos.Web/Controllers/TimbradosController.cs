using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;

namespace PlayaAutos.Web.Controllers
{
    public class TimbradosController : AdminVendedorController
    {
        private readonly ApiService _api;

        public TimbradosController(ApiService api)
        {
            _api = api;
        }

        // ── GET /Timbrados ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            var timbrados = await _api.GetAsync<List<TimbradoListItem>>("api/Timbrados")
                            ?? new List<TimbradoListItem>();

            // Ordenar: activos primero, luego por fecha de vencimiento desc
            timbrados = timbrados
                .OrderByDescending(t => t.Activo)
                .ThenByDescending(t => t.FechaVencimiento)
                .ToList();

            return View(timbrados);
        }

        // ── GET /Timbrados/Crear ─────────────────────────────────────────
        public IActionResult Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            return View(new TimbradoViewModel());
        }

        // ── POST /Timbrados/Crear ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(TimbradoViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            if (vm.FechaVencimiento <= vm.FechaInicio)
                ModelState.AddModelError("FechaVencimiento",
                    "La fecha de vencimiento debe ser posterior a la fecha de inicio.");

            if (vm.NumeroHasta <= vm.NumeroDesde)
                ModelState.AddModelError("NumeroHasta",
                    "El número hasta debe ser mayor al número desde.");

            if (!ModelState.IsValid)
                return View(vm);

            var response = await _api.PostAsync("api/Timbrados", new
            {
                vm.NumeroTimbrado,
                vm.FechaInicio,
                vm.FechaVencimiento,
                vm.NumeroDesde,
                vm.NumeroHasta
            });

            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("número") || detalle.Contains("timbrado")
                    ? "Ya existe un timbrado con ese número."
                    : "Error al registrar el timbrado. Intente nuevamente.");
                return View(vm);
            }

            TempData["Exito"] = $"Timbrado {vm.NumeroTimbrado} registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Timbrados/Desactivar ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!EsAdministrador()) return Forbid();

            var resp = await _api.PutAsync($"api/Timbrados/{id}/desactivar", new { });
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Timbrado desactivado correctamente."
                : "No se pudo desactivar el timbrado.";

            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private bool EsAdministrador() =>
            HttpContext.Session.GetString("Rol") == "AdministradorP";
    }
}