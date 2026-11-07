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
    public class GastosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GastosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/gastos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GastoVehiculoDto>>> GetGastos()
        {
            var gastos = await _context.GastosVehiculo
                .Include(g => g.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(g => g.TipoGasto)
                .OrderByDescending(g => g.Fecha)
                .Select(g => new GastoVehiculoDto
                {
                    GastoId = g.GastoId,
                    VehiculoId = g.VehiculoId,
                    Vehiculo = g.Vehiculo.CodigoInterno + " - " +
                               g.Vehiculo.Modelo.Marca.Nombre + " " +
                               g.Vehiculo.Modelo.Nombre,
                    TipoGasto = g.TipoGasto.Descripcion,
                    Descripcion = g.Descripcion,
                    Monto = g.Monto,
                    Proveedor = g.Proveedor,
                    Fecha = g.Fecha,
                    Usuario = ""
                })
                .ToListAsync();

            return Ok(gastos);
        }

        // GET: api/gastos/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GastoVehiculoDto>> GetGasto(int id)
        {
            var g = await _context.GastosVehiculo
                .Include(g => g.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(g => g.TipoGasto)
                .FirstOrDefaultAsync(g => g.GastoId == id);

            if (g == null) return NotFound();

            return new GastoVehiculoDto
            {
                GastoId = g.GastoId,
                VehiculoId = g.VehiculoId,
                Vehiculo = g.Vehiculo.CodigoInterno + " - " +
                           g.Vehiculo.Modelo.Marca.Nombre + " " +
                           g.Vehiculo.Modelo.Nombre,
                TipoGasto = g.TipoGasto.Descripcion,
                Descripcion = g.Descripcion,
                Monto = g.Monto,
                Proveedor = g.Proveedor,
                Fecha = g.Fecha
            };
        }

        // POST: api/gastos
        [HttpPost]
        public async Task<IActionResult> PostGasto(CrearGastoVehiculoDto dto)
        {
            // Validar vehículo
            var vehiculo = await _context.Vehiculos.FindAsync(dto.VehiculoId);
            if (vehiculo == null)
                return BadRequest("El vehículo no existe.");

            // Validar tipo de gasto
            var tipoGasto = await _context.TiposGasto.FindAsync(dto.TipoGastoId);
            if (tipoGasto == null)
                return BadRequest("El tipo de gasto no existe.");

            // Buscar tipo de movimiento "Egreso por Gasto"
            var tipoMovimiento = await _context.TiposMovimiento
                .FirstOrDefaultAsync(t => t.Signo == "-" && t.Descripcion.Contains("Gasto"));

            if (tipoMovimiento == null)
                return BadRequest("No se encontró tipo de movimiento para gastos.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Crear el gasto
                var gasto = new GastoVehiculo
                {
                    VehiculoId = dto.VehiculoId,
                    TipoGastoId = dto.TipoGastoId,
                    Descripcion = dto.Descripcion,
                    Monto = dto.Monto,
                    Proveedor = dto.Proveedor,
                    Fecha = dto.Fecha,
                    UsuarioRegistro = dto.UsuarioRegistro
                };

                _context.GastosVehiculo.Add(gasto);
                await _context.SaveChangesAsync();

                // 2. Crear movimiento de caja automático (EGRESO)
                var movimiento = new MovimientoCaja
                {
                    Fecha = dto.Fecha,
                    TipoMovimientoId = tipoMovimiento.TipoMovimientoId,
                    Descripcion = $"Gasto: {dto.Descripcion} - {vehiculo.CodigoInterno}",
                    Monto = dto.Monto,
                    GastoId = gasto.GastoId,
                    UsuarioRegistro = dto.UsuarioRegistro,
                    Comentarios = dto.Proveedor != null ? $"Proveedor: {dto.Proveedor}" : null
                };

                _context.MovimientosCaja.Add(movimiento);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetGasto), new { id = gasto.GastoId },
                    new { gasto.GastoId, movimientoId = movimiento.MovimientoId });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error al registrar el gasto.");
            }
        }

        // GET: api/gastos/tipos
        [HttpGet("tipos")]
        public async Task<ActionResult<IEnumerable<TipoGastoDto>>> GetTiposGasto()
        {
            var tipos = await _context.TiposGasto
                .Where(t => t.Activo)
                .Select(t => new TipoGastoDto
                {
                    TipoGastoId = t.TipoGastoId,
                    Descripcion = t.Descripcion
                })
                .ToListAsync();

            return Ok(tipos);
        }
    }
}