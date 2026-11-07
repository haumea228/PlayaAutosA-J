using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ApiService _api;

        public CatalogoController(ApiService api)
        {
            _api = api;
        }

        // GET /Catalogo
        public async Task<IActionResult> Index()
        {
            var raw = await _api.GetAsync<List<JsonElement>>("api/vehiculos/catalogo") ?? new();

            var vehiculos = raw.Select(v => new CatalogoItemViewModel
            {
                VehiculoId    = GetInt(v, "vehiculoId"),
                Marca         = Get(v, "marca"),
                Modelo        = Get(v, "modelo"),
                Anio          = GetInt(v, "anio"),
                Color         = Get(v, "color"),
                Kilometraje   = GetLongNullable(v, "kilometraje"),
                PrecioVenta   = GetLong(v, "precioVenta"),
                Tipo          = Get(v, "tipo"),
                Condicion     = Get(v, "condicion"),
                FotoPrincipal = Get(v, "fotoPrincipal"),
                Descripcion   = Get(v, "descripcion")
            }).ToList();

            return View(vehiculos);
        }

        // GET /Catalogo/IndexCliente
        public async Task<IActionResult> IndexCliente()
        {
            if (!EsCliente()) return RedirectToAction("Login", "Auth");

            var raw = await _api.GetAsync<List<JsonElement>>("api/vehiculos/catalogo") ?? new();
            var vehiculos = raw.Select(v => new CatalogoItemViewModel
            {
                VehiculoId    = GetInt(v, "vehiculoId"),
                Marca         = Get(v, "marca"),
                Modelo        = Get(v, "modelo"),
                Anio          = GetInt(v, "anio"),
                Color         = Get(v, "color"),
                Kilometraje   = GetLongNullable(v, "kilometraje"),
                PrecioVenta   = GetLong(v, "precioVenta"),
                Tipo          = Get(v, "tipo"),
                Condicion     = Get(v, "condicion"),
                FotoPrincipal = Get(v, "fotoPrincipal"),
                Descripcion   = Get(v, "descripcion")
            }).ToList();

            return View(vehiculos);
        }

        // GET /Catalogo/DetalleCliente/5
        public async Task<IActionResult> DetalleCliente(int id)
        {
            if (!EsCliente()) return RedirectToAction("Login", "Auth");

            var raw = await _api.GetAsync<JsonElement>($"api/vehiculos/catalogo/{id}");
            if (raw.ValueKind == JsonValueKind.Undefined) return NotFound();

            var fotos = new List<CatalogoFotoViewModel>();
            if (raw.TryGetProperty("fotos", out var fotosEl) && fotosEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in fotosEl.EnumerateArray())
                {
                    fotos.Add(new CatalogoFotoViewModel
                    {
                        URL         = Get(f, "url"),
                        EsPrincipal = f.TryGetProperty("esPrincipal", out var ep) && ep.GetBoolean(),
                        Orden       = GetInt(f, "orden")
                    });
                }
            }

            var vm = new CatalogoDetalleViewModel
            {
                VehiculoId    = GetInt(raw, "vehiculoId"),
                Marca         = Get(raw, "marca"),
                Modelo        = Get(raw, "modelo"),
                Anio          = GetInt(raw, "anio"),
                Color         = Get(raw, "color"),
                Kilometraje   = GetLongNullable(raw, "kilometraje"),
                PrecioVenta   = GetLong(raw, "precioVenta"),
                Tipo          = Get(raw, "tipo"),
                Condicion     = Get(raw, "condicion"),
                Estado        = Get(raw, "estado"),
                FotoPrincipal = Get(raw, "fotoPrincipal"),
                Descripcion   = Get(raw, "descripcion"),
                Fotos         = fotos
            };

            return View(vm);
        }

        // GET /Catalogo/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var raw = await _api.GetAsync<JsonElement>($"api/vehiculos/catalogo/{id}");
            if (raw.ValueKind == JsonValueKind.Undefined) return NotFound();

            var fotos = new List<CatalogoFotoViewModel>();
            if (raw.TryGetProperty("fotos", out var fotosEl) && fotosEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in fotosEl.EnumerateArray())
                {
                    fotos.Add(new CatalogoFotoViewModel
                    {
                        URL         = Get(f, "url"),
                        EsPrincipal = f.TryGetProperty("esPrincipal", out var ep) && ep.GetBoolean(),
                        Orden       = GetInt(f, "orden")
                    });
                }
            }

            var vm = new CatalogoDetalleViewModel
            {
                VehiculoId    = GetInt(raw, "vehiculoId"),
                Marca         = Get(raw, "marca"),
                Modelo        = Get(raw, "modelo"),
                Anio          = GetInt(raw, "anio"),
                Color         = Get(raw, "color"),
                Kilometraje   = GetLongNullable(raw, "kilometraje"),
                PrecioVenta   = GetLong(raw, "precioVenta"),
                Tipo          = Get(raw, "tipo"),
                Condicion     = Get(raw, "condicion"),
                Estado        = Get(raw, "estado"),
                FotoPrincipal = Get(raw, "fotoPrincipal"),
                Descripcion   = Get(raw, "descripcion"),
                Fotos         = fotos
            };

            return View(vm);
        }

        // ── helpers ──────────────────────────────────────────────────────

        private bool EsCliente() =>
            HttpContext.Session.GetString("Token") != null &&
            HttpContext.Session.GetString("Rol") == "Cliente";

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null)
                return v.ValueKind == JsonValueKind.Number ? v.GetRawText() : v.GetString() ?? "";
            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var v2) && v2.ValueKind != JsonValueKind.Null)
                return v2.ValueKind == JsonValueKind.Number ? v2.GetRawText() : v2.GetString() ?? "";
            return "";
        }

        private static int GetInt(JsonElement el, string prop)
            => int.TryParse(Get(el, prop), out var n) ? n : 0;

        private static long GetLong(JsonElement el, string prop)
            => long.TryParse(Get(el, prop), out var n) ? n : 0;

        private static long? GetLongNullable(JsonElement el, string prop)
        {
            var s = Get(el, prop);
            return long.TryParse(s, out var n) ? n : null;
        }
    }
}
