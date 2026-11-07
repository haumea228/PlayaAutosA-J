using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class VentasController : AdminVendedorController
    {
        private readonly ApiService _api;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        private const long _maxTamanioComprob = 5 * 1024 * 1024; // 5 MB

        public VentasController(ApiService api, IWebHostEnvironment env)
        {
            _api = api;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            var ventas = await _api.GetAsync<List<VentaListItem>>("api/Ventas") ?? new();
            return View(ventas);
        }

        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            var vm = new VentaViewModel { FechaVenta = DateTime.Today };
            await CargarDropdowns(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(VentaViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.MontoEntrada.HasValue && vm.MontoEntrada > vm.MontoTotal)
                ModelState.AddModelError("MontoEntrada", "La entrada no puede superar el monto total.");

            if (vm.SaldoFinanciado.HasValue && vm.CantidadCuotas is null or <= 0)
                ModelState.AddModelError("CantidadCuotas", "Indique la cantidad de cuotas para el saldo financiado.");

            if (vm.Pagos.Count == 0)
                ModelState.AddModelError("", "Debe agregar al menos un pago.");
            else
            {
                var montoEsperado = vm.MontoEntrada ?? vm.MontoTotal;
                var sumaPagos = vm.Pagos.Sum(p => p.Monto);
                if (sumaPagos != montoEsperado)
                    ModelState.AddModelError("", $"La suma de los pagos (Gs. {sumaPagos:N0}) no coincide con el monto de entrada (Gs. {montoEsperado:N0}).");
            }

            if (!ModelState.IsValid) { await CargarDropdowns(vm); return View(vm); }

            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "comprobantes");
            Directory.CreateDirectory(uploadsPath);

            for (int i = 0; i < vm.Pagos.Count; i++)
            {
                var archivo = Request.Form.Files.GetFile($"ComprobanteFiles[{i}]");
                if (archivo != null && archivo.Length > 0)
                {
                    var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                    if (_extensionesPermitidas.Contains(ext) && archivo.Length <= _maxTamanioComprob)
                    {
                        var nombre = $"{Guid.NewGuid()}{ext}";
                        var ruta = Path.Combine(uploadsPath, nombre);
                        await using var stream = new FileStream(ruta, FileMode.Create);
                        await archivo.CopyToAsync(stream);
                        vm.Pagos[i].ComprobanteImagen = $"/uploads/comprobantes/{nombre}";
                    }
                }
            }

            var vendedorId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            var response = await _api.PostAsync("api/Ventas", new
            {
                vm.ClienteId,
                vm.VehiculoId,
                VendedorId = vendedorId,
                vm.TipoVentaId,
                vm.FechaVenta,
                vm.MontoTotal,
                vm.MontoEntrada,
                vm.SaldoFinanciado,
                vm.TasaInteres,
                vm.CantidadCuotas,
                vm.PorcentajeRecargo,
                Pagos = vm.Pagos.Select(p => new {
                    p.FormaPagoId,
                    p.Monto,
                    p.ComprobanteImagen,
                    p.Observacion,
                    p.TasacionVehiculoId
                }).ToList()
            });

            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", string.IsNullOrWhiteSpace(detalle) ? "Error al registrar la venta." : detalle);
                await CargarDropdowns(vm); return View(vm);
            }

            TempData["Exito"] = "Venta registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id, string motivo)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrWhiteSpace(motivo)) { TempData["Error"] = "Debe ingresar un motivo."; return RedirectToAction(nameof(Index)); }
            var resp = await _api.PutAsync($"api/Ventas/{id}/anular", motivo);
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode ? "Venta anulada." : "No se pudo anular.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Pendientes(string? tab)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!PuedeAprobar()) return RedirectToAction("AccesoDenegado", "Auth");

            var ventas = await _api.GetAsync<List<VentaListItem>>("api/Ventas") ?? new();
            var pagos = await _api.GetAsync<List<SolicitudPagoCuotaItem>>("api/Cuotas/solicitudes?estado=Pendiente") ?? new();
            var formas = await _api.GetAsync<List<JsonElement>>("api/Catalogos/formas-pago") ?? new();

            var vm = new PendientesViewModel
            {
                Ventas = ventas.Where(v => v.Estado == "Pendiente").ToList(),
                PagosCuotas = pagos,
                // Los pagos de la app son siempre por transferencia: se preselecciona.
                FormasPago = formas
                    .Select(f => new SelectListItem(Get(f, "descripcion"), Get(f, "formaPagoId"),
                        Get(f, "descripcion").Contains("transfer", StringComparison.OrdinalIgnoreCase)))
                    .ToList(),
                Tab = tab == "cuotas" ? "cuotas" : "ventas"
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarPagoCuota(int id, int formaPagoId, string? observacion)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!PuedeAprobar()) return RedirectToAction("AccesoDenegado", "Auth");

            var resp = await _api.PutAsync($"api/Cuotas/solicitudes/{id}/aprobar",
                new { FormaPagoId = formaPagoId, ObservacionPago = observacion });

            if (resp.IsSuccessStatusCode)
                TempData["Exito"] = "Pago aprobado. La cuota quedó pagada y el cliente ya puede descargar su ticket.";
            else
                TempData["Error"] = await ApiService.LeerMensajeErrorAsync(resp) ?? "No se pudo aprobar el pago.";

            return RedirectToAction(nameof(Pendientes), new { tab = "cuotas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarPagoCuota(int id, string motivo)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            if (!PuedeAprobar()) return RedirectToAction("AccesoDenegado", "Auth");

            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "Ingresá el motivo del rechazo: el cliente lo verá en la app.";
                return RedirectToAction(nameof(Pendientes), new { tab = "cuotas" });
            }

            var resp = await _api.PutAsync($"api/Cuotas/solicitudes/{id}/rechazar", new { Motivo = motivo });

            if (resp.IsSuccessStatusCode)
                TempData["Exito"] = "Pago rechazado. La cuota volvió a quedar pendiente para el cliente.";
            else
                TempData["Error"] = await ApiService.LeerMensajeErrorAsync(resp) ?? "No se pudo rechazar el pago.";

            return RedirectToAction(nameof(Pendientes), new { tab = "cuotas" });
        }

        private bool PuedeAprobar()
        {
            var rol = HttpContext.Session.GetString("Rol");
            return rol == "AdministradorP" || rol == "Cajero";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");
            var resp = await _api.PutAsync($"api/Ventas/{id}/finalizar", new { });
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode ? "Venta finalizada." : "Error al finalizar.";
            return RedirectToAction(nameof(Pendientes));
        }

        private bool EstaAutenticado() => HttpContext.Session.GetString("Token") != null;

        private async Task CargarDropdowns(VentaViewModel vm)
        {
            var clientes = await _api.GetAsync<List<JsonElement>>("api/Clientes") ?? new();
            var vehiculos = await _api.GetAsync<List<JsonElement>>("api/Vehiculos") ?? new();
            var tiposVenta = await _api.GetAsync<List<JsonElement>>("api/Catalogos/tipos-venta") ?? new();
            var formasPago = await _api.GetAsync<List<JsonElement>>("api/Catalogos/formas-pago") ?? new();
            var tasaciones = await _api.GetAsync<List<JsonElement>>("api/Tasaciones") ?? new();

            vm.Clientes = clientes.Where(c => c.TryGetProperty("activo", out var a) && a.GetBoolean())
                .Select(c => new SelectListItem(Get(c, "nombre") + " — " + Get(c, "ci_RUC"), Get(c, "clienteId"))).ToList();

            vm.Vehiculos = vehiculos.Where(v => {
                if (!v.TryGetProperty("estado", out var est)) return false;
                if (est.ValueKind == JsonValueKind.Object) return est.TryGetProperty("permiteVenta", out var pv) && pv.GetBoolean();
                return (est.GetString() ?? "").Equals("Disponible", StringComparison.OrdinalIgnoreCase);
            }).Select(v => new SelectListItem(
                Get(v, "codigoInterno") + " — " + Get(v, "marca") + " " + Get(v, "modelo") + " (" + Get(v, "anio") + ")  Gs. " + (long.TryParse(Get(v, "precioVenta"), out var p) ? p.ToString("N0") : ""),
                Get(v, "vehiculoId"))).ToList();

            vm.TiposVenta = tiposVenta
                .Select(t => new SelectListItem(Get(t, "descripcion"), Get(t, "tipoVentaId")))
                .ToList();

            // 🟢 NUEVO: Cargar TiposVentaData desde la API
            vm.TiposVentaData = tiposVenta.Select(t => new TipoVentaListItem
            {
                TipoVentaId = int.TryParse(Get(t, "tipoVentaId"), out var tid) ? tid : 0,
                Descripcion = Get(t, "descripcion"),
                RequiereFinanciacion = t.TryGetProperty("requiereFinanciacion", out var rf) && rf.GetBoolean()
            }).ToList();

            vm.FormasPago = formasPago.Select(f => new SelectListItem(Get(f, "descripcion"), Get(f, "formaPagoId"))).ToList();

            vm.TasacionesAprobadas = tasaciones.Where(t => Get(t, "estadoTasacion") == "Aprobada")
                .Select(t => new TasacionSelectItem
                {
                    TasacionVehiculoId = int.TryParse(Get(t, "tasacionVehiculoId"), out var tid) ? tid : 0,
                    ClienteId = int.TryParse(Get(t, "clienteId"), out var cid) ? cid : 0,
                    PrecioVenta = long.TryParse(Get(t, "precioVenta"), out var pv) ? pv : 0,
                    ValorTasacion = long.TryParse(Get(t, "valorTasacion"), out var vt) ? vt : 0,
                    Label = Get(t, "marcaVehiculo") + " " + Get(t, "modeloVehiculo") + " (" + Get(t, "anhoVehiculo") + ") — Valor: Gs. " + (long.TryParse(Get(t, "valorTasacion"), out var vt2) ? vt2.ToString("N0") : "")
                }).ToList();
        }

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val)) return val.ValueKind == JsonValueKind.Number ? val.GetRawText() : val.GetString() ?? "";
            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var val2)) return val2.ValueKind == JsonValueKind.Number ? val2.GetRawText() : val2.GetString() ?? "";
            return "";
        }
    }
}