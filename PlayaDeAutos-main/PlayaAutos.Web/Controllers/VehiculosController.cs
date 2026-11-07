using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlayaAutos.Web.Helpers;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;
using System.Text.Json;

namespace PlayaAutos.Web.Controllers
{
    public class VehiculosController : AdminVendedorController
    {
        private readonly ApiService _api;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long _maxTamanioFoto = 5 * 1024 * 1024; // 5 MB

        public VehiculosController(ApiService api, IWebHostEnvironment env)
        {
            _api = api;
            _env = env;
        }

        // ── GET /Vehiculos ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vehiculos = await _api.GetAsync<List<VehiculoListItem>>("api/Vehiculos")
                            ?? new List<VehiculoListItem>();
            return View(vehiculos);
        }

        // ── GET /Vehiculos/ExportarExcel ─────────────────────────────────
        [HttpGet]
        [Route("/Vehiculos/ExportarExcel")]
        public async Task<IActionResult> ExportarExcel(string? busqueda, string? estado)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vehiculos = await _api.GetAsync<List<VehiculoListItem>>("api/Vehiculos")
                            ?? new List<VehiculoListItem>();

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(busqueda))
            {
                var q = busqueda.ToLower();
                vehiculos = vehiculos.Where(v =>
                    (v.CodigoInterno?.ToLower().Contains(q) ?? false) ||
                    (v.Marca?.ToLower().Contains(q) ?? false) ||
                    (v.Modelo?.ToLower().Contains(q) ?? false)
                ).ToList();
            }

            // Filtro por estado
            if (!string.IsNullOrEmpty(estado))
            {
                vehiculos = vehiculos.Where(v =>
                    v.Estado?.Equals(estado, StringComparison.OrdinalIgnoreCase) == true
                ).ToList();
            }

            var datos = vehiculos.Select(v => new Dictionary<string, object>
            {
                ["Código"] = v.CodigoInterno ?? "",
                ["Marca"] = v.Marca ?? "",
                ["Modelo"] = v.Modelo ?? "",
                ["Año"] = v.Anio,
                ["Color"] = v.Color ?? "",
                ["Kilometraje"] = v.Kilometraje?.ToString("N0") ?? "0",
                ["Estado"] = v.Estado ?? "",
                ["Condición"] = v.Condicion ?? "",
                ["Precio Venta"] = v.PrecioVenta
            }).ToList();

            var bytes = ExcelHelper.GenerarExcelDesdeDiccionario(datos, "Vehículos");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Stock_Vehiculos_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ── GET /Vehiculos/Crear ─────────────────────────────────────────
        public async Task<IActionResult> Crear()
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var vm = new VehiculoViewModel { Anio = DateTime.Now.Year };
            await CargarDropdowns(vm);
            return View(vm);
        }

        // ── POST /Vehiculos/Crear ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(VehiculoViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.Fotos != null)
            {
                foreach (var foto in vm.Fotos.Where(f => f.Length > 0))
                {
                    var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
                    if (!_extensionesPermitidas.Contains(ext))
                        ModelState.AddModelError("Fotos", $"Formato no permitido: {foto.FileName}. Use JPG, PNG o WEBP.");

                    if (foto.Length > _maxTamanioFoto)
                        ModelState.AddModelError("Fotos", $"La imagen '{foto.FileName}' supera los 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(vm);
                return View(vm);
            }

            var response = await _api.PostAsync("api/Vehiculos", new
            {
                vm.CodigoInterno,
                vm.ModeloId,
                vm.TipoId,
                vm.CondicionId,
                vm.EstadoId,
                vm.OrigenId,
                vm.Anio,
                vm.Color,
                vm.Kilometraje,
                vm.PrecioVenta,
                vm.CostoAdquisicion,
                vm.Descripcion,
                vm.Transmision,
                vm.Combustible,
                vm.CantidadPuertas,
                vm.Motor,
                vm.Traccion
            });

            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("CodigoInterno")
                    ? "Ya existe un vehículo con ese código interno."
                    : EsMensajeLegible(detalle) ? detalle : "Error al registrar el vehículo. Intente nuevamente.");
                await CargarDropdowns(vm);
                return View(vm);
            }

            var location = response.Headers.Location?.ToString() ?? "";
            if (!int.TryParse(location.Split('/').LastOrDefault(), out int vehiculoId) || vehiculoId == 0)
            {
                TempData["Exito"] = "Vehículo creado. No se pudieron adjuntar las imágenes.";
                return RedirectToAction(nameof(Index));
            }

            if (vm.Fotos != null && vm.Fotos.Any(f => f.Length > 0))
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "vehiculos");
                Directory.CreateDirectory(uploadsPath);

                int orden = 1;
                foreach (var foto in vm.Fotos.Where(f => f.Length > 0))
                {
                    var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
                    var nombreArchivo = $"{Guid.NewGuid()}{ext}";
                    var rutaFisica = Path.Combine(uploadsPath, nombreArchivo);

                    await using var stream = new FileStream(rutaFisica, FileMode.Create);
                    await foto.CopyToAsync(stream);

                    await _api.PostAsync($"api/Vehiculos/{vehiculoId}/fotos", new
                    {
                        url = $"/uploads/vehiculos/{nombreArchivo}",
                        nombreArchivo,
                        esPrincipal = orden == 1,
                        orden
                    });
                    orden++;
                }
            }

            TempData["Exito"] = "Vehículo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Vehiculos/Editar/{id} ───────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var dto = await _api.GetAsync<JsonElement>($"api/Vehiculos/{id}");
            if (dto.ValueKind == JsonValueKind.Undefined) return NotFound();

            var vm = new VehiculoViewModel
            {
                VehiculoId = id,
                CodigoInterno = Get(dto, "codigoInterno"),
                MarcaId = int.TryParse(Get(dto, "marcaId"), out var m) ? m : 0,
                ModeloId = int.TryParse(Get(dto, "modeloId"), out var mo) ? mo : 0,
                TipoId = int.TryParse(Get(dto, "tipoId"), out var t) ? t : 0,
                CondicionId = int.TryParse(Get(dto, "condicionId"), out var c) ? c : 0,
                EstadoId = int.TryParse(Get(dto, "estadoId"), out var e) ? e : 0,
                OrigenId = int.TryParse(Get(dto, "origenId"), out var o) ? o : 0,
                Anio = int.TryParse(Get(dto, "anio"), out var a) ? a : DateTime.Now.Year,
                Color = Get(dto, "color"),
                Kilometraje = long.TryParse(Get(dto, "kilometraje"), out var km) ? km : null,
                PrecioVenta = long.TryParse(Get(dto, "precioVenta"), out var pv) ? pv : 0,
                CostoAdquisicion = long.TryParse(Get(dto, "costoAdquisicion"), out var ca) ? ca : null,
                Descripcion = Get(dto, "descripcion"),
                Transmision = NullSiVacio(Get(dto, "transmision")),
                Combustible = NullSiVacio(Get(dto, "combustible")),
                CantidadPuertas = int.TryParse(Get(dto, "cantidadPuertas"), out var pu) ? pu : null,
                Motor = NullSiVacio(Get(dto, "motor")),
                Traccion = NullSiVacio(Get(dto, "traccion")),
            };

            if (dto.TryGetProperty("fotos", out var fotosEl) && fotosEl.ValueKind == JsonValueKind.Array)
            {
                vm.FotosExistentes = fotosEl.EnumerateArray().Select(f => new FotoExistenteItem
                {
                    FotoId = f.TryGetProperty("fotoId", out var fi) ? fi.GetInt32() : 0,
                    URL = f.TryGetProperty("url", out var fu) ? fu.GetString() ?? "" : "",
                    NombreArchivo = f.TryGetProperty("nombreArchivo", out var fn) ? fn.GetString() ?? "" : "",
                    EsPrincipal = f.TryGetProperty("esPrincipal", out var ep) && ep.GetBoolean()
                }).ToList();
            }

            await CargarDropdowns(vm);
            return View(vm);
        }

        // ── POST /Vehiculos/Editar/{id} ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, VehiculoViewModel vm)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            if (vm.Fotos != null)
            {
                foreach (var foto in vm.Fotos.Where(f => f.Length > 0))
                {
                    var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
                    if (!_extensionesPermitidas.Contains(ext))
                        ModelState.AddModelError("Fotos", $"Formato no permitido: {foto.FileName}.");
                    if (foto.Length > _maxTamanioFoto)
                        ModelState.AddModelError("Fotos", $"'{foto.FileName}' supera los 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDropdowns(vm);
                vm.VehiculoId = id;
                return View(vm);
            }

            var resp = await _api.PutAsync($"api/Vehiculos/{id}", new
            {
                vm.CodigoInterno,
                vm.ModeloId,
                vm.TipoId,
                vm.CondicionId,
                vm.EstadoId,
                vm.OrigenId,
                vm.Anio,
                vm.Color,
                vm.Kilometraje,
                vm.PrecioVenta,
                vm.CostoAdquisicion,
                vm.Descripcion,
                vm.Transmision,
                vm.Combustible,
                vm.CantidadPuertas,
                vm.Motor,
                vm.Traccion
            });

            if (!resp.IsSuccessStatusCode)
            {
                var detalle = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", detalle.Contains("CodigoInterno")
                    ? "Ya existe un vehículo con ese código interno."
                    : EsMensajeLegible(detalle) ? detalle : "Error al actualizar el vehículo.");
                await CargarDropdowns(vm);
                vm.VehiculoId = id;
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.FotoIdsEliminar ?? ""))
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "vehiculos");
                foreach (var parte in (vm.FotoIdsEliminar ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var segmentos = parte.Trim().Split('|');
                    if (!int.TryParse(segmentos[0], out int fotoId)) continue;
                    var archivo = segmentos.Length > 1 ? segmentos[1] : "";

                    await _api.DeleteAsync($"api/Vehiculos/{id}/fotos/{fotoId}");

                    if (!string.IsNullOrEmpty(archivo))
                    {
                        var rutaFisica = Path.Combine(uploadsPath, archivo);
                        if (System.IO.File.Exists(rutaFisica))
                            System.IO.File.Delete(rutaFisica);
                    }
                }
            }

            if (vm.Fotos != null && vm.Fotos.Any(f => f.Length > 0))
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "vehiculos");
                Directory.CreateDirectory(uploadsPath);

                var fotosActuales = await _api.GetAsync<JsonElement>($"api/Vehiculos/{id}");
                int orden = 1;
                if (fotosActuales.TryGetProperty("fotos", out var fa) && fa.ValueKind == JsonValueKind.Array)
                    orden = fa.GetArrayLength() + 1;

                bool hayPrincipal = orden > 1;

                foreach (var foto in vm.Fotos.Where(f => f.Length > 0))
                {
                    var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
                    var nombreArchivo = $"{Guid.NewGuid()}{ext}";
                    var rutaFisica = Path.Combine(uploadsPath, nombreArchivo);

                    await using var stream = new FileStream(rutaFisica, FileMode.Create);
                    await foto.CopyToAsync(stream);

                    await _api.PostAsync($"api/Vehiculos/{id}/fotos", new
                    {
                        url = $"/uploads/vehiculos/{nombreArchivo}",
                        nombreArchivo,
                        esPrincipal = !hayPrincipal && orden == 1,
                        orden
                    });
                    orden++;
                    hayPrincipal = true;
                }
            }

            TempData["Exito"] = "Vehículo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Vehiculos/Eliminar ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!EstaAutenticado()) return RedirectToAction("Login", "Auth");

            var resp = await _api.DeleteAsync($"api/Vehiculos/{id}");
            TempData[resp.IsSuccessStatusCode ? "Exito" : "Error"] = resp.IsSuccessStatusCode
                ? "Vehículo eliminado correctamente."
                : "No se pudo eliminar el vehículo. Puede tener ventas o citas asociadas.";

            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private bool EstaAutenticado() =>
            HttpContext.Session.GetString("Token") != null;

        private async Task CargarDropdowns(VehiculoViewModel vm)
        {
            var marcas = await _api.GetAsync<List<JsonElement>>("api/Marcas") ?? new();
            var modelos = await _api.GetAsync<List<JsonElement>>("api/Modelos") ?? new();
            var tipos = await _api.GetAsync<List<JsonElement>>("api/Catalogos/tipos-vehiculo") ?? new();
            var condiciones = await _api.GetAsync<List<JsonElement>>("api/Catalogos/condiciones-vehiculo") ?? new();
            var estados = await _api.GetAsync<List<JsonElement>>("api/Catalogos/estados-vehiculo") ?? new();
            var origenes = await _api.GetAsync<List<JsonElement>>("api/Catalogos/origenes-vehiculo") ?? new();

            vm.Marcas = marcas.Select(m => new SelectListItem(Get(m, "nombre"), Get(m, "marcaId"))).ToList();
            vm.Tipos = tipos.Select(t => new SelectListItem(Get(t, "descripcion"), Get(t, "tipoId"))).ToList();
            vm.Condiciones = condiciones.Select(c => new SelectListItem(Get(c, "descripcion"), Get(c, "condicionId"))).ToList();
            vm.Estados = estados.Select(e => new SelectListItem(Get(e, "descripcion"), Get(e, "estadoId"))).ToList();
            vm.Origenes = origenes.Select(o => new SelectListItem(Get(o, "descripcion"), Get(o, "origenId"))).ToList();

            vm.Modelos = modelos.Select(m => new SelectListItem
            {
                Text = Get(m, "nombre"),
                Value = Get(m, "modeloId"),
                Group = new SelectListGroup { Name = Get(m, "marcaId") }
            }).ToList();

            // Valores permitidos de la ficha técnica: los define la API (única fuente).
            var opciones = await _api.GetAsync<JsonElement>("api/Vehiculos/ficha-tecnica/opciones");
            vm.Transmisiones = Opciones(opciones, "transmisiones");
            vm.Combustibles = Opciones(opciones, "combustibles");
            vm.Tracciones = Opciones(opciones, "tracciones");
        }

        private static List<SelectListItem> Opciones(JsonElement opciones, string prop)
        {
            if (opciones.ValueKind != JsonValueKind.Object ||
                !opciones.TryGetProperty(prop, out var lista) ||
                lista.ValueKind != JsonValueKind.Array)
                return new();

            return lista.EnumerateArray()
                .Select(v => v.GetString() ?? "")
                .Where(v => v.Length > 0)
                .Select(v => new SelectListItem(v, v))
                .ToList();
        }

        private static string? NullSiVacio(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

        /// <summary>Mensajes cortos de validación de la API (no JSON ni HTML).</summary>
        private static bool EsMensajeLegible(string detalle) =>
            detalle.Length is > 0 and < 300 && !detalle.TrimStart().StartsWith('{') && !detalle.TrimStart().StartsWith('<');

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