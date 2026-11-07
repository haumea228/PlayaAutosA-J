using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Helpers;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class CajaController : AdminVendedorController
    {
        private readonly ApiService _api;

        public CajaController(ApiService api)
        {
            _api = api;
        }

        // GET /Caja
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, string? signo)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            desde ??= DateTime.Today.AddDays(-30);
            hasta ??= DateTime.Today;

            var queryString = $"api/Caja/movimientos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&soloAbiertos=true";
            if (!string.IsNullOrEmpty(signo))
                queryString += $"&signo={signo}";

            var movimientos = await _api.GetAsync<List<CajaListItem>>(queryString)
                              ?? new List<CajaListItem>();

            var balance = await _api.GetAsync<BalanceViewModel>(
                $"api/Caja/balance?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}")
                ?? new BalanceViewModel();

            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.Signo = signo;
            ViewBag.Balance = balance;

            return View(movimientos);
        }

        // GET /Caja/Crear
        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vm = new CrearMovimientoViewModel();
            await CargarDropdowns(vm);
            return View(vm);
        }

        // POST /Caja/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearMovimientoViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var esEgresoGasto = vm.TipoMovimientoId == vm.TipoEgresoGastoId;

            if (esEgresoGasto)
            {
                if (!vm.VehiculoId.HasValue || vm.VehiculoId == 0)
                    ModelState.AddModelError("VehiculoId", "Debe seleccionar un vehículo para el gasto.");
                if (!vm.TipoGastoId.HasValue || vm.TipoGastoId == 0)
                    ModelState.AddModelError("TipoGastoId", "Debe seleccionar el tipo de gasto.");
            }

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(vm);
                return View(vm);
            }

            var vendedorId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            HttpResponseMessage resp;

            if (esEgresoGasto)
            {
                resp = await _api.PostAsync("api/Gastos", new
                {
                    VehiculoId = vm.VehiculoId!.Value,
                    TipoGastoId = vm.TipoGastoId!.Value,
                    vm.Descripcion,
                    vm.Monto,
                    vm.Proveedor,
                    vm.Fecha,
                    UsuarioRegistro = vendedorId
                });

                TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                    ? "Egreso registrado y gasto de vehículo creado correctamente."
                    : "Error al registrar el gasto de vehículo.";
            }
            else
            {
                resp = await _api.PostAsync("api/Caja/movimientos", new
                {
                    vm.Fecha,
                    vm.TipoMovimientoId,
                    vm.Descripcion,
                    vm.Monto,
                    vm.Comentarios,
                    UsuarioRegistro = vendedorId
                });

                TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                    ? "Movimiento registrado correctamente."
                    : "Error al registrar el movimiento.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST /Caja/Cerrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var resp = await _api.PostAsync("api/Caja/cerrar", new { });

            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Caja cerrada correctamente."
                : "No hay movimientos para cerrar.";

            return RedirectToAction(nameof(Index));
        }

        // GET /Caja/Historial
        public async Task<IActionResult> Historial()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var cierres = await _api.GetAsync<List<CierreCajaViewModel>>("api/Caja/cierres")
                          ?? new List<CierreCajaViewModel>();
            return View(cierres);
        }

        // GET /Caja/DetalleCierre/{id}
        public async Task<IActionResult> DetalleCierre(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var movimientos = await _api.GetAsync<List<CajaListItem>>(
                $"api/Caja/cierres/{id}/movimientos") ?? new List<CajaListItem>();

            ViewBag.CierreId = id;
            return View(movimientos);
        }

        // GET /Caja/ExportarExcel
        [HttpGet]
        [Route("/Caja/ExportarExcel")]
        public async Task<IActionResult> ExportarExcel(DateTime? desde, DateTime? hasta, string? signo)
        {
            desde ??= DateTime.Today.AddDays(-30);
            hasta ??= DateTime.Today;

            var url = $"api/Caja/movimientos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
            if (!string.IsNullOrEmpty(signo))
                url += $"&signo={signo}";

            var movimientos = await _api.GetAsync<List<CajaListItem>>(url)
                              ?? new List<CajaListItem>();

            var datos = movimientos.Select(m => new Dictionary<string, object>
            {
                ["Fecha"] = m.Fecha.ToString("dd/MM/yyyy HH:mm"),
                ["Tipo"] = m.TipoMovimiento,
                ["Signo"] = m.Signo,
                ["Descripción"] = m.Descripcion,
                ["Referencia"] = m.Referencia ?? "",
                ["Detalle"] = m.Comentarios ?? "",
                ["Monto"] = m.Monto
            }).ToList();

            var bytes = ExcelHelper.GenerarExcelDesdeDiccionario(datos, "Caja");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Caja_{desde:yyyyMMdd}_al_{hasta:yyyyMMdd}.xlsx");
        }
        // GET /Caja/ExportarCierreExcel/{id}
        [HttpGet]
        [Route("/Caja/ExportarCierreExcel/{id}")]
        public async Task<IActionResult> ExportarCierreExcel(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var movimientos = await _api.GetAsync<List<CajaListItem>>(
                $"api/Caja/cierres/{id}/movimientos") ?? new List<CajaListItem>();

            var datos = movimientos.Select(m => new Dictionary<string, object>
            {
                ["Fecha"] = m.Fecha.ToString("dd/MM/yyyy HH:mm"),
                ["Tipo"] = m.TipoMovimiento,
                ["Signo"] = m.Signo,
                ["Descripción"] = m.Descripcion,
                ["Referencia"] = m.Referencia ?? "",
                ["Monto"] = m.Monto
            }).ToList();

            var bytes = ExcelHelper.GenerarExcelDesdeDiccionario(datos, $"Cierre_{id}");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Cierre_{id}.xlsx");
        }
        // Helpers
        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private async Task CargarDropdowns(CrearMovimientoViewModel vm)
        {
            try
            {
                var tipos = await _api.GetAsync<List<JsonElement>>("api/Caja/tipos") ?? new();

                if (tipos.Any())
                {
                    vm.TiposMovimiento = tipos
                        .Select(t => new SelectListItem(
                            $"{Get(t, "descripcion")} ({Get(t, "signo")})",
                            Get(t, "tipoMovimientoId")))
                        .ToList();

                    var egresoGasto = tipos.FirstOrDefault(t =>
                        Get(t, "signo") == "-" &&
                        Get(t, "descripcion").Contains("Gasto", StringComparison.OrdinalIgnoreCase));

                    if (int.TryParse(Get(egresoGasto, "tipoMovimientoId"), out var id))
                        vm.TipoEgresoGastoId = id;
                }
                else
                {
                    vm.TiposMovimiento = ObtenerTiposPorDefecto();
                    vm.TipoEgresoGastoId = 6;
                }
            }
            catch
            {
                vm.TiposMovimiento = ObtenerTiposPorDefecto();
                vm.TipoEgresoGastoId = 6;
            }

            try
            {
                var vehiculos = await _api.GetAsync<List<JsonElement>>("api/Vehiculos") ?? new();
                vm.Vehiculos = vehiculos
                    .Select(v => new SelectListItem(
                        $"{Get(v, "codigoInterno")} — {Get(v, "marca")} {Get(v, "modelo")}",
                        Get(v, "vehiculoId")))
                    .ToList();
            }
            catch { }

            try
            {
                var tiposGasto = await _api.GetAsync<List<JsonElement>>("api/Gastos/tipos") ?? new();
                vm.TiposGasto = tiposGasto
                    .Select(t => new SelectListItem(
                        Get(t, "descripcion"),
                        Get(t, "tipoGastoId")))
                    .ToList();
            }
            catch { }
        }

        private static List<SelectListItem> ObtenerTiposPorDefecto()
        {
            return new()
            {
                new SelectListItem("Ingreso por Venta (+)", "3"),
                new SelectListItem("Cobro de Cuota (+)", "4"),
                new SelectListItem("Ingreso Manual (+)", "5"),
                new SelectListItem("Egreso por Gasto (-)", "6"),
                new SelectListItem("Egreso Manual (-)", "7")
            };
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