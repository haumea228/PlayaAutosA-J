using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Helpers;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class CuotasController : AdminVendedorController
    {
        private readonly ApiService _api;

        public CuotasController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(string? estado)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var endpoint = "api/Cuotas" + (!string.IsNullOrEmpty(estado) ? $"?estado={estado}" : "");
            var cuotas = await _api.GetAsync<List<CuotaListItem>>(endpoint)
                         ?? new List<CuotaListItem>();

            ViewBag.FiltroActual = estado ?? "todas";
            return View(cuotas);
        }

        public async Task<IActionResult> Pagar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var dto = await _api.GetAsync<JsonElement>($"api/Cuotas/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            if (Get(dto, "estado") == "EnRevision")
            {
                TempData["Error"] = "El cliente informó un pago de esta cuota desde la app. Revisalo en Pendientes → Pagos de cuotas.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new PagarCuotaViewModel
            {
                CuotaId = id,
                NumeroCuota = int.Parse(Get(dto, "numeroCuota")),
                Monto = long.Parse(Get(dto, "monto")),
                FechaVencimiento = DateTime.TryParse(Get(dto, "fechaVencimiento"), out var fv) ? fv : DateTime.Now,
                Cliente = Get(dto, "cliente"),
                Vehiculo = Get(dto, "vehiculo"),
                CantidadCuotas = int.TryParse(Get(dto, "cantidadCuotas"), out var cc) ? cc : null,
                FechaPago = DateTime.Today,
                MontoPagado = long.Parse(Get(dto, "monto")),
                RecargoAutomatico = decimal.TryParse(Get(dto, "recargoAutomatico"), out var ra) ? ra : null
            };

            await CargarFormasPago(vm);
            return View(vm);
        }
        [HttpGet]
        [Route("/Cuotas/ExportarExcel")]
        public async Task<IActionResult> ExportarExcel()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var cuotas = await _api.GetAsync<List<CuotaListItem>>("api/Cuotas")
                         ?? new List<CuotaListItem>();

            var grupos = cuotas
                .GroupBy(c => new { c.VentaId, c.Cliente, c.Vehiculo, c.CantidadCuotas })
                .ToList();

            var hojas = new Dictionary<string, List<Dictionary<string, object>>>();

            foreach (var grupo in grupos)
            {
                var nombreHoja = $"{grupo.Key.Cliente}_{grupo.Key.VentaId}";
                if (nombreHoja.Length > 31) nombreHoja = nombreHoja.Substring(0, 31);

                var datos = new List<Dictionary<string, object>>();

                // Usar las MISMAS columnas que la tabla para que todo quede alineado
                var columnas = new[] { "N° Cuota", "Vencimiento", "Monto Cuota", "Monto Pagado", "Recargo", "Estado", "Fecha Pago" };

                // 🟢 Fila 1: CLIENTE
                datos.Add(CrearFilaInfo(columnas, "CLIENTE", grupo.Key.Cliente));

                // 🟢 Fila 2: VENTA
                datos.Add(CrearFilaInfo(columnas, "VENTA #", grupo.Key.VentaId.ToString()));

                // 🟢 Fila 3: VEHÍCULO
                datos.Add(CrearFilaInfo(columnas, "VEHÍCULO", grupo.Key.Vehiculo ?? "Sin especificar"));

                // 🟢 Fila 4: TOTAL CUOTAS
                datos.Add(CrearFilaInfo(columnas, "TOTAL CUOTAS", grupo.Key.CantidadCuotas?.ToString() ?? "0"));

                // 🟢 Fila 5: PENDIENTES / PAGADAS
                var pagadas = grupo.Count(c => c.Estado == "Pagada");
                var pendientes = grupo.Count(c => c.Estado != "Pagada");
                datos.Add(CrearFilaInfo(columnas, "RESUMEN", $"Pagadas: {pagadas} | Pendientes: {pendientes}"));

                // 🟢 Fila 6: Vacía (separador)
                datos.Add(CrearFilaVacia(columnas));

                // 🟢 Fila 7: Encabezados de la tabla
                datos.Add(new Dictionary<string, object>
                {
                    ["N° Cuota"] = "N° Cuota",
                    ["Vencimiento"] = "Vencimiento",
                    ["Monto Cuota"] = "Monto Cuota",
                    ["Monto Pagado"] = "Monto Pagado",
                    ["Recargo"] = "Recargo",
                    ["Estado"] = "Estado",
                    ["Fecha Pago"] = "Fecha Pago"
                });

                // 🟢 Datos de cada cuota
                foreach (var c in grupo.OrderBy(c => c.NumeroCuota))
                {
                    datos.Add(new Dictionary<string, object>
                    {
                        ["N° Cuota"] = c.NumeroCuota,
                        ["Vencimiento"] = c.FechaVencimiento.ToString("dd/MM/yyyy"),
                        ["Monto Cuota"] = c.Monto,
                        ["Monto Pagado"] = c.MontoPagado ?? 0,
                        ["Recargo"] = c.MontoRecargo ?? 0,
                        ["Estado"] = c.Estado,
                        ["Fecha Pago"] = c.FechaPago?.ToString("dd/MM/yyyy") ?? "Pendiente"
                    });
                }

                hojas[nombreHoja] = datos;
            }

            var bytes = ExcelHelper.GenerarExcelMultiHoja(hojas);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Cuotas_Completo_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // Helper para crear filas de información
        private Dictionary<string, object> CrearFilaInfo(string[] columnas, string etiqueta, string valor)
        {
            var fila = new Dictionary<string, object>();
            foreach (var col in columnas)
            {
                fila[col] = "";
            }
            fila[columnas[0]] = etiqueta;
            fila[columnas[1]] = valor;
            return fila;
        }

        private Dictionary<string, object> CrearFilaVacia(string[] columnas)
        {
            var fila = new Dictionary<string, object>();
            foreach (var col in columnas)
            {
                fila[col] = "";
            }
            return fila;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagar(int id, PagarCuotaViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            // Siempre forzar monto original (el campo es solo lectura en el form)
            vm.MontoPagado = vm.Monto;
            ModelState.Remove(nameof(vm.MontoPagado));

            if (!ModelState.IsValid)
            {
                await CargarFormasPago(vm);
                return View(vm);
            }

            var vendedorId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            string? comprobanteBase64 = null;
            if (vm.Comprobante != null && vm.Comprobante.Length > 0)
            {
                using var ms = new MemoryStream();
                await vm.Comprobante.CopyToAsync(ms);
                var mimeType = vm.Comprobante.ContentType;
                comprobanteBase64 = $"data:{mimeType};base64,{Convert.ToBase64String(ms.ToArray())}";
            }

            var resp = await _api.PutAsync($"api/Cuotas/{id}/pagar", new
            {
                vm.FechaPago,
                vm.MontoPagado,
                vm.MontoRecargo,
                vm.FormaPagoId,
                vm.ObservacionPago,
                ComprobanteImagen = comprobanteBase64,
                UsuarioRegistro = vendedorId
            });

            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = await ApiService.LeerMensajeErrorAsync(resp) ?? "Error al registrar el pago.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Exito"] = "Cuota pagada correctamente.";
            TempData["TicketCuotaId"] = id;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DescargarTicket(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var response = await _api.GetRawAsync($"api/Cuotas/{id}/ticket");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo generar el ticket de pago.";
                return RedirectToAction(nameof(Index));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var nombre = response.Content.Headers.ContentDisposition?.FileNameStar
                      ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                      ?? $"Ticket-Cuota-{id}.pdf";

            return File(bytes, "application/pdf", nombre);
        }

        /// <summary>
        /// Comprobante de un pago informado desde la app. La API lo sirve solo
        /// con JWT, así que el navegador lo pide acá y la web lo reenvía.
        /// </summary>
        public async Task<IActionResult> ComprobanteSolicitud(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var response = await _api.GetRawAsync($"api/Cuotas/solicitudes/{id}/comprobante");
            if (!response.IsSuccessStatusCode) return NotFound();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return File(bytes, contentType);
        }

        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private async Task CargarFormasPago(PagarCuotaViewModel vm)
        {
            var formas = await _api.GetAsync<List<JsonElement>>("api/Catalogos/formas-pago") ?? new();
            vm.FormasPago = formas
                .Select(f => new SelectListItem(Get(f, "descripcion"), Get(f, "formaPagoId")))
                .ToList();
        }

        private static string Get(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var val))
                return val.ValueKind == JsonValueKind.Number ? val.GetRawText() : val.GetString() ?? "";
            var camel = char.ToLower(prop[0]) + prop[1..];
            if (el.TryGetProperty(camel, out var val2))
                return val2.ValueKind == JsonValueKind.Number ? val2.GetRawText() : val2.GetString() ?? "";
            return "";
        }
    }
}
