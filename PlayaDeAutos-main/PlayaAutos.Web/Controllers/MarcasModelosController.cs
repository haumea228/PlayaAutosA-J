using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class MarcasModelosController : AdminVendedorController
    {
        private readonly ApiService _api;

        public MarcasModelosController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var marcas = await _api.GetAsync<List<MarcaItem>>("api/Marcas") ?? new();
            var modelos = await _api.GetAsync<List<ModeloItem>>("api/Modelos") ?? new();

            return View(new MarcasModelosViewModel { Marcas = marcas, Modelos = modelos });
        }

        [HttpPost]
        public async Task<IActionResult> CrearMarca([FromBody] MarcaItem dto)
        {
            if (!EstaAutenticado()) return Json(new { ok = false });
            var resp = await _api.PostAsync("api/Marcas", new { dto.Nombre });
            return Json(new { ok = resp.IsSuccessStatusCode });
        }

        [HttpPost]
        public async Task<IActionResult> EditarMarca(int id, [FromBody] MarcaItem dto)
        {
            if (!EstaAutenticado()) return Json(new { ok = false });
            var resp = await _api.PutAsync($"api/Marcas/{id}", new { dto.Nombre });
            return Json(new { ok = resp.IsSuccessStatusCode });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarMarca(int id)
        {
            if (!EstaAutenticado()) return Json(new { ok = false });
            var resp = await _api.DeleteAsync($"api/Marcas/{id}");
            return Json(new { ok = resp.IsSuccessStatusCode });
        }

        [HttpPost]
        public async Task<IActionResult> CrearModelo([FromBody] ModeloItem dto)
        {
            if (!EstaAutenticado()) return Json(new { ok = false });
            var resp = await _api.PostAsync("api/Modelos", new { dto.Nombre, dto.MarcaId });
            return Json(new { ok = resp.IsSuccessStatusCode });
        }

        [HttpPost]
        public async Task<IActionResult> EditarModelo(int id, [FromBody] ModeloItem dto)
        {
            if (!EstaAutenticado()) return Json(new { ok = false });
            var resp = await _api.PutAsync($"api/Modelos/{id}", new { dto.Nombre, dto.MarcaId });
            return Json(new { ok = resp.IsSuccessStatusCode });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarModelo(int id)
        {
            if (!EstaAutenticado()) return Json(new { ok = false });
            var resp = await _api.DeleteAsync($"api/Modelos/{id}");
            return Json(new { ok = resp.IsSuccessStatusCode });
        }

        private bool EstaAutenticado() => HttpContext.Session.GetString("Token") != null;
    }
}