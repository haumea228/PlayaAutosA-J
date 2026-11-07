using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class MisCitasController : Controller
    {
        private readonly ApiService _api;

        public MisCitasController(ApiService api)
        {
            _api = api;
        }

        private bool EsCliente() =>
            HttpContext.Session.GetString("Token") != null &&
            HttpContext.Session.GetString("Rol") == "Cliente";

        // GET /MisCitas
        public async Task<IActionResult> Index()
        {
            if (!EsCliente()) return RedirectToAction("Login", "Auth");

            var citas = await _api.GetAsync<List<JsonElement>>("api/micuenta/citas") ?? new();

            var lista = citas.Select(c => new CitaListItem
            {
                CitaId       = GetInt(c, "citaId"),
                Cliente      = GetStr(c, "cliente"),
                Vehiculo     = GetStr(c, "vehiculo"),
                Vendedor     = GetStr(c, "vendedor"),
                Estado       = GetStr(c, "estado"),
                EstadoColor  = GetStr(c, "estadoColor"),
                EstadoCitaId = GetInt(c, "estadoCitaId"),
                FechaHora    = GetDateTime(c, "fechaHora"),
                TipoCita     = GetStr(c, "tipoCita"),
                Observaciones = GetStr(c, "observaciones")
            }).ToList();

            return View(lista);
        }

        // GET /MisCitas/Crear?vehiculoId=5
        [HttpGet]
        public async Task<IActionResult> Crear(int? vehiculoId = null, string? vehiculoInfo = null)
        {
            if (!EsCliente()) return RedirectToAction("Login", "Auth");

            var perfil = await _api.GetAsync<JsonElement>("api/micuenta/perfil");

            var model = new AgendarCitaViewModel
            {
                VehiculoId   = vehiculoId,
                VehiculoInfo = vehiculoInfo,
                FechaHora    = DateTime.Now.AddHours(1),
                TipoCita     = "Consulta"
            };

            if (perfil.ValueKind != JsonValueKind.Undefined)
            {
                model.Nombre    = GetStr(perfil, "nombre");
                model.CI_RUC    = GetStr(perfil, "ci_RUC") is { Length: > 0 } ci ? ci : GetStr(perfil, "cI_RUC");
                model.Telefono  = GetStr(perfil, "telefono");
                model.Direccion = GetStr(perfil, "direccion");
            }

            return View(model);
        }

        // POST /MisCitas/Crear
        [HttpPost]
        public async Task<IActionResult> Crear(AgendarCitaViewModel model)
        {
            if (!EsCliente()) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return View(model);

            if (model.FechaHora <= DateTime.Now)
            {
                ModelState.AddModelError("FechaHora", "La fecha y hora deben ser futuras.");
                return View(model);
            }

            var response = await _api.PostAsync("api/micuenta/citas/agendar", new
            {
                vehiculoId    = model.VehiculoId,
                fechaHora     = model.FechaHora,
                tipoCita      = model.TipoCita,
                observaciones = model.Observaciones,
                nombre        = model.Nombre,
                ci_RUC        = model.CI_RUC,
                telefono      = model.Telefono,
                direccion     = model.Direccion
            });

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", err.Trim('"'));
                return View(model);
            }

            TempData["Exito"] = "¡Cita agendada exitosamente! Nos pondremos en contacto a la brevedad.";
            return RedirectToAction("Index");
        }

        // POST /MisCitas/Cancelar
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (!EsCliente()) return RedirectToAction("Login", "Auth");

            var response = await _api.PutAsync<object>($"api/micuenta/citas/{id}/cancelar", new { });
            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo cancelar la cita.";
            else
                TempData["Exito"] = "Cita cancelada correctamente.";

            return RedirectToAction("Index");
        }

        // ── helpers ──────────────────────────────────────────────────────

        private static string GetStr(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null)
                return v.GetString() ?? "";
            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var v2) && v2.ValueKind != JsonValueKind.Null)
                return v2.GetString() ?? "";
            return "";
        }

        private static int GetInt(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v) && v.TryGetInt32(out int n)) return n;
            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var v2) && v2.TryGetInt32(out int n2)) return n2;
            return 0;
        }

        private static DateTime GetDateTime(JsonElement el, string prop)
        {
            var s = GetStr(el, prop);
            return DateTime.TryParse(s, out var dt) ? dt : DateTime.MinValue;
        }
    }
}
