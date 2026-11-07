using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Models;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor,Cajero")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ventas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VentaDto>>> GetVentas()
        {
            return await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Vehiculo).ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(v => v.Vendedor)
                .Include(v => v.TipoVenta)
                .Include(v => v.PagosVenta).ThenInclude(p => p.FormaPago)
                .Select(v => new VentaDto
                {
                    VentaId = v.VentaId,
                    Cliente = v.Cliente.Nombre,
                    Vehiculo = v.Vehiculo.Modelo.Marca.Nombre + " " + v.Vehiculo.Modelo.Nombre,
                    Vendedor = v.Vendedor.UsuarioNombre,
                    TipoVenta = v.TipoVenta.Descripcion,
                    FormasPago = string.Join(", ", v.PagosVenta.Select(p => p.FormaPago.Descripcion)),
                    FechaVenta = v.FechaVenta,
                    MontoTotal = v.MontoTotal,
                    Estado = v.Estado,
                    Pagos = v.PagosVenta.Select(p => new PagoVentaDto
                    {
                        PagoVentaId = p.PagoVentaId,
                        FormaPago = p.FormaPago.Descripcion,
                        Monto = p.Monto,
                        ComprobanteImagen = p.ComprobanteImagen,
                        Observacion = p.Observacion,
                        TasacionVehiculoId = p.TasacionVehiculoId
                    }).ToList()
                })
                .ToListAsync();
        }

        // GET: api/ventas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VentaDto>> GetVenta(int id)
        {
            var v = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Vehiculo).ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(v => v.Vendedor)
                .Include(v => v.TipoVenta)
                .Include(v => v.PagosVenta).ThenInclude(p => p.FormaPago)
                .Include(v => v.PagosVenta).ThenInclude(p => p.TasacionVehiculo)
                .FirstOrDefaultAsync(v => v.VentaId == id);

            if (v == null) return NotFound();

            return new VentaDto
            {
                VentaId = v.VentaId,
                Cliente = v.Cliente.Nombre,
                Vehiculo = v.Vehiculo.Modelo.Marca.Nombre + " " + v.Vehiculo.Modelo.Nombre,
                Vendedor = v.Vendedor.UsuarioNombre,
                TipoVenta = v.TipoVenta.Descripcion,
                FormasPago = string.Join(", ", v.PagosVenta.Select(p => p.FormaPago.Descripcion)),
                FechaVenta = v.FechaVenta,
                MontoTotal = v.MontoTotal,
                Estado = v.Estado,
                Pagos = v.PagosVenta.Select(p => new PagoVentaDto
                {
                    PagoVentaId = p.PagoVentaId,
                    FormaPago = p.FormaPago.Descripcion,
                    Monto = p.Monto,
                    ComprobanteImagen = p.ComprobanteImagen,
                    Observacion = p.Observacion,
                    TasacionVehiculoId = p.TasacionVehiculoId,
                    VehiculoTasado = p.TasacionVehiculo != null
                        ? $"{p.TasacionVehiculo.MarcaVehiculo} {p.TasacionVehiculo.ModeloVehiculo} {p.TasacionVehiculo.AnhoVehiculo}"
                        : null
                }).ToList()
            };
        }

        // POST: api/ventas
        [HttpPost]
        public async Task<IActionResult> PostVenta(CrearVentaDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ── Obtener IDs necesarios ──────────────────────────────────────
                var tipoCredito = await _context.TiposVenta
                    .FirstOrDefaultAsync(t => t.RequiereFinanciacion);

                // ── Validar suma de pagos ────────────────────────────────────
                if (dto.Pagos.Count == 0)
                    return BadRequest("Debe especificar al menos un pago.");

                var montoEsperado = dto.MontoEntrada ?? dto.MontoTotal;
                var sumaPagos = dto.Pagos.Sum(p => p.Monto);
                if (sumaPagos != montoEsperado)
                    return BadRequest($"La suma de los pagos ({sumaPagos}) no coincide con el monto de entrada ({montoEsperado}).");

                // validacion para que pagos no supere monto entrada
                foreach (var pago in dto.Pagos)
                {
                    if (pago.Monto > montoEsperado)
                        return BadRequest($"El pago de Gs. {pago.Monto:N0} supera el monto de entrada ({montoEsperado:N0}).");
                }

                // validacion para que permuta no supere monto total
                var montoPermuta = dto.Pagos
                    .Where(p => p.TasacionVehiculoId.HasValue)
                    .Sum(p => p.Monto);

                if (montoPermuta > dto.MontoTotal)
                    return BadRequest($"La permuta (Gs. {montoPermuta:N0}) no puede superar el monto total de la venta (Gs. {dto.MontoTotal:N0}).");

                // Validar vehículo si existe y esta disponible y permite venta
                var vehiculo = await _context.Vehiculos
                    .Include(v => v.Estado)
                    .FirstOrDefaultAsync(v => v.VehiculoId == dto.VehiculoId);

                if (vehiculo == null)
                    return BadRequest("El vehículo no existe.");

                if (!vehiculo.Estado.PermiteVenta)
                    return BadRequest($"El vehículo no está disponible para la venta. Estado actual: {vehiculo.Estado.Descripcion}");

                // validar que exista el cliente
                if (!await _context.Clientes.AnyAsync(c => c.ClienteId == dto.ClienteId))
                    return BadRequest("El cliente no existe.");

                // valida que las tasaciones referenciadas si existan
                var tasacionIds = dto.Pagos
                    .Where(p => p.TasacionVehiculoId.HasValue)
                    .Select(p => p.TasacionVehiculoId!.Value)
                    .ToList();

                foreach (var tasId in tasacionIds)
                {
                    var tas = await _context.TasacionesVehiculo.FindAsync(tasId);
                    if (tas == null)
                        return BadRequest($"La tasación {tasId} no existe.");
                    if (tas.EstadoTasacion != "Aprobada")
                        return BadRequest($"La tasación {tasId} debe estar en estado Aprobada para usarse como permuta.");
                }

                // ── Crear venta ──────────────────────────────────────────────
                var venta = new Venta
                {
                    ClienteId = dto.ClienteId,
                    VehiculoId = dto.VehiculoId,
                    VendedorId = dto.VendedorId,
                    TipoVentaId = dto.TipoVentaId,
                    FechaVenta = dto.FechaVenta,
                    MontoTotal = dto.MontoTotal,
                    MontoEntrada = dto.MontoEntrada,
                    SaldoFinanciado = dto.SaldoFinanciado,
                    TasaInteres = dto.TasaInteres,
                    CantidadCuotas = dto.CantidadCuotas,
                    Estado = "Pendiente",  // Pendiente hasta que Cajero finalice
                    PorcentajeRecargo = dto.PorcentajeRecargo // nuevo para recargo automatico
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                // ── Crear pagos ──────────────────────────────────────────────
                foreach (var pagoDto in dto.Pagos)
                {
                    _context.PagosVenta.Add(new PagoVenta
                    {
                        VentaId = venta.VentaId,
                        FormaPagoId = pagoDto.FormaPagoId,
                        Monto = pagoDto.Monto,
                        ComprobanteImagen = pagoDto.ComprobanteImagen,
                        Observacion = pagoDto.Observacion,
                        TasacionVehiculoId = pagoDto.TasacionVehiculoId
                    });
                }

                // ── 🟢 Cambiar estado del vehículo a Vendido AHORA ──────────
                var estadoVendido = await _context.EstadosVehiculo
                    .FirstOrDefaultAsync(e => e.Descripcion == "Vendido");

                if (estadoVendido != null)
                    vehiculo.EstadoId = estadoVendido.EstadoId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetVenta), new { id = venta.VentaId },
                    new { venta.VentaId, mensaje = "Venta creada en estado Pendiente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al procesar la venta: {ex.Message}");
            }
        }

        // PUT: api/ventas/5/finalizar
        [HttpPut("{id}/finalizar")]
        [Authorize(Roles = "AdministradorP,Cajero")]
        public async Task<IActionResult> FinalizarVenta(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Vehiculo).ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                    .Include(v => v.PagosVenta)
                    .FirstOrDefaultAsync(v => v.VentaId == id);

                if (venta == null) return NotFound();
                if (venta.Estado != "Pendiente")
                    return BadRequest("Solo se pueden finalizar ventas pendientes.");

                var tipoCredito = await _context.TiposVenta
                    .FirstOrDefaultAsync(t => t.RequiereFinanciacion);

                // ── Validar timbrado activo ──────────────────────────────────
                var timbrado = await _context.Timbrados
                    .FirstOrDefaultAsync(t =>
                        t.Activo &&
                        t.FechaInicio <= DateTime.Now &&
                        t.FechaVencimiento >= DateTime.Now &&
                        t.UltimoNumeroUsado < t.NumeroHasta);

                if (timbrado == null)
                    return BadRequest("No hay timbrado activo disponible.");

                // ── Generar factura ─────────────────────────────────────────
                timbrado.UltimoNumeroUsado++;
                var numeroFactura = $"{timbrado.NumeroTimbrado}-{timbrado.UltimoNumeroUsado:D8}";

                var subtotal = (long)Math.Round(venta.MontoTotal / 1.10m);
                var iva = venta.MontoTotal - subtotal;

                _context.Facturas.Add(new Factura
                {
                    VentaId = venta.VentaId,
                    TimbradoId = timbrado.TimbradoId,
                    NumeroFactura = numeroFactura,
                    FechaEmision = DateTime.Now,
                    Subtotal = subtotal,
                    IVA = iva,
                    Total = venta.MontoTotal,
                    FechaGeneracion = DateTime.Now,
                    UsuarioGeneracion = venta.VendedorId
                });

                // ── Generar nota de crédito por cada permuta ─────────────────
                foreach (var pago in venta.PagosVenta.Where(p => p.TasacionVehiculoId.HasValue))
                {
                    var tasPerm = await _context.TasacionesVehiculo.FindAsync(pago.TasacionVehiculoId!.Value);
                    if (tasPerm == null) continue;

                    timbrado.UltimoNumeroUsado++;
                    var numeroNota = $"NC-{timbrado.NumeroTimbrado}-{timbrado.UltimoNumeroUsado:D8}";

                    _context.NotasCredito.Add(new NotaCredito
                    {
                        VentaId = venta.VentaId,
                        TimbradoId = timbrado.TimbradoId,
                        NumeroNota = numeroNota,
                        FechaEmision = DateTime.Now,
                        Motivo = $"Permuta - {tasPerm.MarcaVehiculo} {tasPerm.ModeloVehiculo} ({tasPerm.AnhoVehiculo})",
                        Monto = pago.Monto,
                        VehiculoId = venta.VehiculoId,
                        TasacionVehiculoId = pago.TasacionVehiculoId,
                        FechaGeneracion = DateTime.Now,
                        UsuarioGeneracion = venta.VendedorId
                    });
                }

                // ── Generar cuotas si es crédito ─────────────────────────────
                if (venta.CantidadCuotas.HasValue && venta.CantidadCuotas > 0 && venta.SaldoFinanciado.HasValue)
                {
                    var montoCuota = venta.SaldoFinanciado.Value / venta.CantidadCuotas.Value;
                    for (int i = 1; i <= venta.CantidadCuotas.Value; i++)
                    {
                        _context.Cuotas.Add(new Cuota
                        {
                            VentaId = venta.VentaId,
                            NumeroCuota = i,
                            Monto = montoCuota,
                            FechaVencimiento = venta.FechaVenta.AddMonths(i),
                            Estado = "Pendiente"
                        });
                    }
                }

                // ── Registrar movimiento de caja (INGRESO) ─────────────────
                var tipoIngreso = await _context.TiposMovimiento
                    .FirstOrDefaultAsync(t => t.Signo == "+");

                if (tipoIngreso != null)
                {
                    long montoRealCaja;
                    string descripcionCaja;
                    bool esCredito = tipoCredito != null && venta.TipoVentaId == tipoCredito.TipoVentaId;

                    if (esCredito)
                    {
                        montoRealCaja = venta.MontoEntrada ?? 0;
                        descripcionCaja = $"Venta #{venta.VentaId} - Entrada crédito";
                    }
                    else
                    {
                        montoRealCaja = venta.MontoTotal;
                        descripcionCaja = $"Venta #{venta.VentaId} - Contado";
                    }

                    var montoPermuta = venta.PagosVenta
                        .Where(p => p.TasacionVehiculoId.HasValue)
                        .Sum(p => p.Monto);
                    montoRealCaja -= montoPermuta;

                    string? comentarioCaja = null;
                    if (montoPermuta > 0)
                    {
                        var tasacionesIds = venta.PagosVenta
                            .Where(p => p.TasacionVehiculoId.HasValue)
                            .Select(p => $"Tasación #{p.TasacionVehiculoId}");
                        comentarioCaja = $"Total venta: Gs. {venta.MontoTotal:N0} | " +
                                         $"Permuta: Gs. {montoPermuta:N0} ({string.Join(", ", tasacionesIds)}) | " +
                                         $"Efectivo: Gs. {montoRealCaja:N0}";
                    }

                    if (montoRealCaja > 0)
                    {
                        _context.MovimientosCaja.Add(new MovimientoCaja
                        {
                            Fecha = DateTime.Now,
                            TipoMovimientoId = tipoIngreso.TipoMovimientoId,
                            Descripcion = descripcionCaja,
                            Monto = montoRealCaja,
                            VentaId = venta.VentaId,
                            UsuarioRegistro = venta.VendedorId,
                            Comentarios = comentarioCaja
                        });
                    }
                }

                // ── Procesar vehículos de permuta (reactivar o crear) ────────
                var estadoDisponible = await _context.EstadosVehiculo
                    .FirstOrDefaultAsync(e => e.Descripcion == "Disponible");

                var tasacionIds = venta.PagosVenta
                    .Where(p => p.TasacionVehiculoId.HasValue)
                    .Select(p => p.TasacionVehiculoId!.Value)
                    .ToList();

                foreach (var tasId in tasacionIds)
                {
                    var tas = await _context.TasacionesVehiculo.FindAsync(tasId);
                    if (tas == null) continue;
                    tas.EstadoTasacion = "Usada";

                    if (tas.VehiculoId.HasValue)
                    {
                        var vehiculoExistente = await _context.Vehiculos.FindAsync(tas.VehiculoId.Value);
                        if (vehiculoExistente != null && estadoDisponible != null)
                        {
                            vehiculoExistente.EstadoId = estadoDisponible.EstadoId;
                            vehiculoExistente.Kilometraje = (long)tas.KilometrajeVehiculo;
                            vehiculoExistente.PrecioVenta = tas.PrecioVenta;
                            vehiculoExistente.Color = tas.ColorVehiculo;
                        }
                    }
                    else if (tas.ModeloId.HasValue && tas.TipoId.HasValue &&
                             tas.CondicionId.HasValue && tas.OrigenId.HasValue &&
                             estadoDisponible != null)
                    {
                        var nuevoVehiculo = new Vehiculo
                        {
                            CodigoInterno = $"TAV-{tasId}",
                            ModeloId = tas.ModeloId.Value,
                            TipoId = tas.TipoId.Value,
                            CondicionId = tas.CondicionId.Value,
                            OrigenId = tas.OrigenId.Value,
                            EstadoId = estadoDisponible.EstadoId,
                            Anio = tas.AnhoVehiculo,
                            Color = tas.ColorVehiculo,
                            Kilometraje = (long)tas.KilometrajeVehiculo,
                            PrecioVenta = tas.PrecioVenta,
                            CostoAdquisicion = tas.ValorTasacion,
                            FechaAlta = DateTime.Now,
                            UsuarioAlta = venta.VendedorId
                        };
                        _context.Vehiculos.Add(nuevoVehiculo);
                        await _context.SaveChangesAsync();
                        tas.VehiculoId = nuevoVehiculo.VehiculoId;
                    }
                }

                // ── Cambiar estado del vehículo a Vendido ────────────────────
                var estadoVendido = await _context.EstadosVehiculo
                    .FirstOrDefaultAsync(e => e.Descripcion == "Vendido");

                if (estadoVendido != null)
                    venta.Vehiculo.EstadoId = estadoVendido.EstadoId;

                // ── Liquidar consigna si aplica ──────────────────────────────
                var vehiculo = venta.Vehiculo;
                if (vehiculo.ConsignanteId.HasValue)
                {
                    var contrato = await _context.ContratosConsigna
                        .FirstOrDefaultAsync(c => c.VehiculoId == vehiculo.VehiculoId && c.Estado == "Vigente");

                    if (contrato != null)
                    {
                        var precioCliente = vehiculo.CostoAdquisicion ?? 0;
                        var comision = (long)(precioCliente * (contrato.PorcentajeComision / 100m));
                        var pagoAlCliente = precioCliente - comision;

                        contrato.Estado = "Vendido";

                        var tipoEgreso = await _context.TiposMovimiento
                            .FirstOrDefaultAsync(t => t.Signo == "-");

                        if (tipoEgreso != null && precioCliente > 0)
                        {
                            _context.MovimientosCaja.Add(new MovimientoCaja
                            {
                                Fecha = DateTime.Now,
                                TipoMovimientoId = tipoEgreso.TipoMovimientoId,
                                Descripcion = $"Pago consignante - Venta #{venta.VentaId} - {vehiculo.Modelo?.Marca?.Nombre} {vehiculo.Modelo?.Nombre}",
                                Monto = pagoAlCliente,
                                VentaId = venta.VentaId,
                                UsuarioRegistro = venta.VendedorId,
                                Comentarios = $"Comisión ({contrato.PorcentajeComision}%): Gs. {comision:N0} | Dueño recibe: Gs. {pagoAlCliente:N0}"
                            });
                        }

                        vehiculo.ConsignanteId = null;
                        vehiculo.PorcentajeComision = null;
                        vehiculo.FechaConsigna = null;
                    }
                }

                // ── Marcar venta como finalizada ─────────────────────────────
                venta.Estado = "Registrada";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Venta finalizada correctamente", numeroFactura });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al finalizar la venta: {ex.Message}");
            }
        }

        // PUT: api/ventas/5/anular
        [HttpPut("{id}/anular")]
        public async Task<IActionResult> AnularVenta(int id, [FromBody] string motivo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Vehiculo)
                    .Include(v => v.MovimientosCaja)
                    .Include(v => v.PagosVenta)
                    .FirstOrDefaultAsync(v => v.VentaId == id);

                if (venta == null) return NotFound();
                if (venta.Estado == "Anulada") return BadRequest("La venta ya está anulada.");

                // ── Calcular cuánto dinero entró realmente en caja ──────────────
                var tipoCredito = await _context.TiposVenta
                    .FirstOrDefaultAsync(t => t.RequiereFinanciacion);

                bool esCredito = tipoCredito != null && venta.TipoVentaId == tipoCredito.TipoVentaId;

                // Monto que ingresó en caja al momento de la venta
                long montoIngresado;
                if (esCredito)
                {
                    montoIngresado = venta.MontoEntrada ?? 0;
                }
                else
                {
                    montoIngresado = venta.MontoTotal;
                }

                // Restar el valor de las permutas (nunca entró ese dinero)
                var montoPermuta = venta.PagosVenta
                    .Where(p => p.TasacionVehiculoId != null)
                    .Sum(p => p.Monto);

                montoIngresado -= montoPermuta;

                // ──  Crear EGRESO por anulación (cambia signo) ────
                // busca por signo -
                if (montoIngresado > 0)
                {
                    var tipoEgreso = await _context.TiposMovimiento
                        .FirstOrDefaultAsync(t => t.Signo == "-");

                    if (tipoEgreso == null)
                    {
                        return BadRequest("No se encontró el tipo de movimiento 'Egreso' en la base de datos.");
                    }

                    _context.MovimientosCaja.Add(new MovimientoCaja
                    {
                        Fecha = DateTime.Now,
                        TipoMovimientoId = tipoEgreso.TipoMovimientoId,
                        Descripcion = $"Anulación Venta #{venta.VentaId}",
                        Monto = montoIngresado,
                        VentaId = venta.VentaId,
                        UsuarioRegistro = venta.VendedorId,
                        Comentarios = $"Motivo: {motivo}"
                    });
                }

                // ── Marcar venta como anulada ───────────────────────────────────
                venta.Estado = "Anulada";
                venta.FechaAnulacion = DateTime.Now;
                venta.MotivoAnulacion = motivo;

                // ── Revertir estado del vehículo ────────────────────────────────
                var estadoDisponible = await _context.EstadosVehiculo
                    .FirstOrDefaultAsync(e => e.Descripcion == "Disponible");
                if (estadoDisponible != null)
                    venta.Vehiculo.EstadoId = estadoDisponible.EstadoId;

                // ── Anular cuotas pendientes ────────────────────────────────────
                var cuotasPendientes = await _context.Cuotas
                    .Where(c => c.VentaId == id &&
                                (c.Estado == EstadosCuota.Pendiente || c.Estado == EstadosCuota.EnRevision))
                    .ToListAsync();

                foreach (var cuota in cuotasPendientes)
                {
                    cuota.Estado = EstadosCuota.Anulada;
                }

                // Pagos informados desde la app que seguían sin revisar: se cierran
                // como rechazados para que no queden colgados en Pendientes.
                var solicitudesAbiertas = await _context.SolicitudesPagoCuota
                    .Where(s => s.Cuota.VentaId == id && s.Estado == EstadosSolicitudPago.Pendiente)
                    .ToListAsync();

                foreach (var solicitud in solicitudesAbiertas)
                {
                    solicitud.Estado = EstadosSolicitudPago.Rechazada;
                    solicitud.MotivoRechazo = "La venta fue anulada.";
                    solicitud.FechaRevision = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Venta anulada correctamente", montoReintegrado = montoIngresado });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al anular la venta: {ex.Message}");
            }
        }
    }
}