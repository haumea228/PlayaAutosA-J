using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class DashboardController : AdminVendedorController
    {
        private readonly ApiService _api;

        public DashboardController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Token") == null)
                return RedirectToAction("Login", "Auth");

            ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
            ViewBag.Rol = HttpContext.Session.GetString("Rol");

            // Valores por defecto
            ViewBag.VehiculosDisponibles = 0;
            ViewBag.ClientesRegistrados = 0;
            ViewBag.VentasDelMes = 0;
            ViewBag.CitasPendientes = 0;
            ViewBag.BalanceCaja = 0L;
            ViewBag.UltimasActividades = new List<ActividadDashboard>();

            try
            {
                // Vehículos disponibles
                var vehiculos = await _api.GetAsync<List<JsonElement>>("api/Vehiculos") ?? new();
                ViewBag.VehiculosDisponibles = vehiculos.Count(v =>
                    v.TryGetProperty("estado", out var e) &&
                    (e.GetString()?.Equals("Disponible", StringComparison.OrdinalIgnoreCase) == true ||
                     e.GetString()?.Equals("Reservado", StringComparison.OrdinalIgnoreCase) == true));

                // Clientes registrados
                var clientes = await _api.GetAsync<List<JsonElement>>("api/Clientes") ?? new();
                ViewBag.ClientesRegistrados = clientes.Count;

                // Ventas del mes
                var ventas = await _api.GetAsync<List<JsonElement>>("api/Ventas") ?? new();
                ViewBag.VentasDelMes = ventas.Count(v =>
                    v.TryGetProperty("fechaVenta", out var fv) &&
                    DateTime.TryParse(fv.GetString(), out var fecha) &&
                    fecha.Month == DateTime.Now.Month &&
                    fecha.Year == DateTime.Now.Year);

                // Citas pendientes = Pendiente + Confirmada (aún no realizadas)
                var citas = await _api.GetAsync<List<JsonElement>>("api/Citas") ?? new();
                ViewBag.CitasPendientes = citas.Count(c =>
                {
                    if (!c.TryGetProperty("estado", out var e)) return false;
                    var estado = e.GetString() ?? "";
                    return estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)
                        || estado.Equals("Confirmada", StringComparison.OrdinalIgnoreCase);
                });

                // Balance de caja
                var balance = await _api.GetAsync<JsonElement>("api/Caja/balance");
                if (balance.ValueKind != JsonValueKind.Undefined)
                {
                    ViewBag.BalanceCaja = balance.TryGetProperty("saldo", out var s) ? s.GetInt64() : 0;
                }

                // Últimas actividades
                var actividades = new List<ActividadDashboard>();

                // Últimas ventas
                var ultimasVentas = ventas
                    .OrderByDescending(v => v.TryGetProperty("fechaVenta", out var fv) ? fv.GetString() : "")
                    .Take(3);
                foreach (var v in ultimasVentas)
                {
                    actividades.Add(new ActividadDashboard
                    {
                        Icono = "fa-file-invoice-dollar",
                        Color = "#22c55e",
                        Descripcion = $"Venta: {Get(v, "cliente")} - {Get(v, "vehiculo")}",
                        Fecha = v.TryGetProperty("fechaVenta", out var fv2) ? fv2.GetString() ?? "" : "",
                        Monto = v.TryGetProperty("montoTotal", out var m) ? m.GetInt64() : 0
                    });
                }

                // Últimos gastos
                var gastos = await _api.GetAsync<List<JsonElement>>("api/Gastos") ?? new();
                var ultimosGastos = gastos
                    .OrderByDescending(g => g.TryGetProperty("fecha", out var fg) ? fg.GetString() : "")
                    .Take(2);
                foreach (var g in ultimosGastos)
                {
                    actividades.Add(new ActividadDashboard
                    {
                        Icono = "fa-receipt",
                        Color = "#ef4444",
                        Descripcion = $"Gasto: {Get(g, "descripcion")} - {Get(g, "vehiculo")}",
                        Fecha = g.TryGetProperty("fecha", out var fg2) ? fg2.GetString() ?? "" : "",
                        Monto = g.TryGetProperty("monto", out var mg) ? mg.GetInt64() : 0
                    });
                }

                ViewBag.UltimasActividades = actividades
                    .OrderByDescending(a => a.Fecha)
                    .Take(5)
                    .ToList();

            }
            catch
            {
                // Si falla la API, se quedan los valores por defecto (0)
            }

            return View();
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

    public class ActividadDashboard
    {
        public string Icono { get; set; } = "fa-circle";
        public string Color { get; set; } = "var(--pa-muted)";
        public string Descripcion { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public long Monto { get; set; }
    }
}