using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Models;
using System.Security.Claims;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor,Cajero")]
    public class CajaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CajaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/caja/movimientos
        [HttpGet("movimientos")]
        public async Task<ActionResult<IEnumerable<MovimientoCajaDto>>> GetMovimientos(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? tipoId,
            [FromQuery] bool? soloAbiertos,
            [FromQuery] string? signo)
        {
            var query = _context.MovimientosCaja
                .Include(m => m.TipoMovimiento)
                .Include(m => m.Venta).ThenInclude(v => v.Cliente)
                .Include(m => m.Cuota)
                .Include(m => m.GastoVehiculo).ThenInclude(g => g.Vehiculo)
                    .ThenInclude(v => v.Modelo).ThenInclude(mo => mo.Marca)
                .AsQueryable();

            if (soloAbiertos == true)
                query = query.Where(m => m.CierreId == null);

            if (desde.HasValue)
                query = query.Where(m => m.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(m => m.Fecha <= hasta.Value.AddDays(1));

            if (tipoId.HasValue)
                query = query.Where(m => m.TipoMovimientoId == tipoId.Value);

            //Filtro por signo: "+" = todos los ingresos, "-" = todos los egresos
            if (!string.IsNullOrEmpty(signo))
            {
                if (signo == "Ingreso")
                    query = query.Where(m => m.TipoMovimiento!.Signo == "+");
                else if (signo == "Egreso")
                    query = query.Where(m => m.TipoMovimiento!.Signo == "-");
            }

            var movimientos = await query
                .OrderByDescending(m => m.Fecha)
                .Select(m => new MovimientoCajaDto
                {
                    MovimientoId = m.MovimientoId,
                    Fecha = m.Fecha,
                    TipoMovimiento = m.TipoMovimiento.Descripcion,
                    Signo = m.TipoMovimiento.Signo,
                    Descripcion = m.Descripcion,
                    Monto = m.Monto,
                    Referencia = m.Venta != null
                        ? $"Venta #{m.VentaId} - {m.Venta.Cliente.Nombre}"
                        : m.Cuota != null
                            ? $"Cuota #{m.Cuota.NumeroCuota} - Venta #{m.Cuota.VentaId}"
                            : m.GastoVehiculo != null
                                ? $"Gasto #{m.GastoId} - {m.GastoVehiculo.Vehiculo.CodigoInterno} ({m.GastoVehiculo.Vehiculo.Modelo.Marca.Nombre} {m.GastoVehiculo.Vehiculo.Modelo.Nombre})"
                                : null,
                    Comentarios = m.Comentarios,
                    Usuario = ""
                })
                .ToListAsync();

            return Ok(movimientos);
        }

        // GET: api/caja/movimientos/{id}
        [HttpGet("movimientos/{id}")]
        public async Task<ActionResult<MovimientoCajaDto>> GetMovimiento(int id)
        {
            var m = await _context.MovimientosCaja
                .Include(m => m.TipoMovimiento)
                .Include(m => m.Venta).ThenInclude(v => v.Cliente)
                .Include(m => m.GastoVehiculo).ThenInclude(g => g.Vehiculo)
                    .ThenInclude(v => v.Modelo).ThenInclude(mo => mo.Marca)
                .FirstOrDefaultAsync(m => m.MovimientoId == id);

            if (m == null) return NotFound();

            return new MovimientoCajaDto
            {
                MovimientoId = m.MovimientoId,
                Fecha = m.Fecha,
                TipoMovimiento = m.TipoMovimiento.Descripcion,
                Signo = m.TipoMovimiento.Signo,
                Descripcion = m.Descripcion,
                Monto = m.Monto,
                Referencia = m.Venta != null
                    ? $"Venta #{m.VentaId}"
                    : m.GastoVehiculo != null
                        ? $"Gasto #{m.GastoId} - {m.GastoVehiculo.Vehiculo.CodigoInterno}"
                        : null,
                Comentarios = m.Comentarios
            };
        }

        // POST: api/caja/movimientos
        [HttpPost("movimientos")]
        public async Task<IActionResult> PostMovimiento(CrearMovimientoCajaDto dto)
        {
            var tipo = await _context.TiposMovimiento.FindAsync(dto.TipoMovimientoId);
            if (tipo == null)
                return BadRequest("Tipo de movimiento no válido.");

            var movimiento = new MovimientoCaja
            {
                Fecha = dto.Fecha,
                TipoMovimientoId = dto.TipoMovimientoId,
                Descripcion = dto.Descripcion,
                Monto = dto.Monto,
                VentaId = dto.VentaId,
                CuotaId = dto.CuotaId,
                GastoId = dto.GastoId,
                FormaPagoId = dto.FormaPagoId,
                UsuarioRegistro = dto.UsuarioRegistro,
                Comentarios = dto.Comentarios
            };

            _context.MovimientosCaja.Add(movimiento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.MovimientoId },
                new { movimiento.MovimientoId });
        }

        // GET: api/caja/balance
        [HttpGet("balance")]
        public async Task<ActionResult<BalanceCajaDto>> GetBalance(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            var query = _context.MovimientosCaja
                .Include(m => m.TipoMovimiento)
                .Where(m => m.CierreId == null && m.TipoMovimiento!.AfectaCaja)
                .AsQueryable();

            if (desde.HasValue)
                query = query.Where(m => m.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(m => m.Fecha <= hasta.Value.AddDays(1));

            var movimientos = await query.ToListAsync();

            var ingresos = movimientos
                .Where(m => m.TipoMovimiento.Signo == "+")
                .Sum(m => m.Monto);

            var egresos = movimientos
                .Where(m => m.TipoMovimiento.Signo == "-")
                .Sum(m => m.Monto);

            return new BalanceCajaDto
            {
                TotalIngresos = ingresos,
                TotalEgresos = egresos,
                Saldo = ingresos - egresos,
                CantidadMovimientos = movimientos.Count
            };
        }

        // GET: api/caja/tipos
        [HttpGet("tipos")]
        public async Task<ActionResult<IEnumerable<TipoMovimientoDto>>> GetTiposMovimiento()
        {
            var tipos = await _context.TiposMovimiento
                .Select(t => new TipoMovimientoDto
                {
                    TipoMovimientoId = t.TipoMovimientoId,
                    Descripcion = t.Descripcion,
                    Signo = t.Signo
                })
                .ToListAsync();

            return Ok(tipos);
        }

        // POST: api/caja/cerrar
        [HttpPost("cerrar")]
        [Authorize(Roles = "AdministradorP")]
        public async Task<IActionResult> CerrarCaja()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var abiertos = await _context.MovimientosCaja
                .Include(m => m.TipoMovimiento)
                .Where(m => m.CierreId == null)
                .ToListAsync();

            if (!abiertos.Any())
                return BadRequest("No hay movimientos para cerrar.");

            var ingresos = abiertos
                .Where(m => m.TipoMovimiento!.Signo == "+" || m.TipoMovimiento!.Descripcion == "+")
                .Sum(m => m.Monto);
            var egresos = abiertos
                .Where(m => m.TipoMovimiento!.Signo == "-" || m.TipoMovimiento!.Descripcion == "-")
                .Sum(m => m.Monto);

            var cierre = new CierreCaja
            {
                FechaCierre = DateTime.Now,
                UsuarioCierre = usuarioId,
                TotalIngresos = ingresos,
                TotalEgresos = egresos,
                SaldoNeto = ingresos - egresos,
                CantidadMovimientos = abiertos.Count,
                Observacion = null
            };
            _context.CierresCaja.Add(cierre);
            await _context.SaveChangesAsync();

            foreach (var m in abiertos)
                m.CierreId = cierre.CierreId;

            await _context.SaveChangesAsync();

            return Ok(new CierreCajaDto
            {
                CierreId = cierre.CierreId,
                FechaCierre = cierre.FechaCierre,
                TotalIngresos = cierre.TotalIngresos,
                TotalEgresos = cierre.TotalEgresos,
                SaldoNeto = cierre.SaldoNeto,
                CantidadMovimientos = cierre.CantidadMovimientos
            });
        }

        // GET: api/caja/cierres
        [HttpGet("cierres")]
        public async Task<ActionResult<IEnumerable<CierreCajaDto>>> GetCierres()
        {
            return await _context.CierresCaja
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaCierre)
                .Select(c => new CierreCajaDto
                {
                    CierreId = c.CierreId,
                    FechaCierre = c.FechaCierre,
                    Usuario = c.Usuario.UsuarioNombre,
                    TotalIngresos = c.TotalIngresos,
                    TotalEgresos = c.TotalEgresos,
                    SaldoNeto = c.SaldoNeto,
                    CantidadMovimientos = c.CantidadMovimientos,
                    Observacion = c.Observacion
                })
                .ToListAsync();
        }

        // GET: api/caja/cierres/{id}/movimientos
        [HttpGet("cierres/{id}/movimientos")]
        public async Task<ActionResult> GetMovimientosPorCierre(int id)
        {
            return Ok(await _context.MovimientosCaja
                .Include(m => m.TipoMovimiento)
                .Include(m => m.Venta).ThenInclude(v => v.Cliente)
                .Include(m => m.Cuota)
                .Include(m => m.GastoVehiculo).ThenInclude(g => g.Vehiculo)
                    .ThenInclude(v => v.Modelo).ThenInclude(mo => mo.Marca)
                .Where(m => m.CierreId == id)
                .Select(m => new MovimientoCajaDto
                {
                    MovimientoId = m.MovimientoId,
                    Fecha = m.Fecha,
                    TipoMovimiento = m.TipoMovimiento!.Descripcion,
                    Signo = m.TipoMovimiento!.Signo,
                    Descripcion = m.Descripcion,
                    Monto = m.Monto,
                    Referencia = m.Venta != null
                        ? $"Venta #{m.VentaId} - {m.Venta.Cliente.Nombre}"
                        : m.Cuota != null
                            ? $"Cuota #{m.Cuota.NumeroCuota} - Venta #{m.Cuota.VentaId}"
                            : m.GastoVehiculo != null
                                ? $"Gasto #{m.GastoId} - {m.GastoVehiculo.Vehiculo.CodigoInterno}"
                                : null,
                    Comentarios = m.Comentarios,
                    Usuario = ""
                })
                .OrderBy(m => m.Fecha)
                .ToListAsync());
        }
    }
}