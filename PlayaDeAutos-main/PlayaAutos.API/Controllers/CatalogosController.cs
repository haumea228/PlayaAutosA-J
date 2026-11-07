using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.Models;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor,Cajero")]
    public class CatalogosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CatalogosController(AppDbContext context)
        {
            _context = context;
        }

        // ─── TIPOS DE VEHÍCULO ───────────────────────────────────────

        [HttpGet("tipos-vehiculo")]
        public async Task<IActionResult> GetTiposVehiculo() =>
            Ok(await _context.TiposVehiculo.Where(t => t.Activo).ToListAsync());

        [HttpPost("tipos-vehiculo")]
        public async Task<IActionResult> PostTipoVehiculo([FromBody] string descripcion)
        {
            _context.TiposVehiculo.Add(new TipoVehiculo { Descripcion = descripcion });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── CONDICIONES DE VEHÍCULO ─────────────────────────────────

        [HttpGet("condiciones-vehiculo")]
        public async Task<IActionResult> GetCondiciones() =>
            Ok(await _context.CondicionesVehiculo.Where(c => c.Activo).ToListAsync());

        [HttpPost("condiciones-vehiculo")]
        public async Task<IActionResult> PostCondicion([FromBody] string descripcion)
        {
            _context.CondicionesVehiculo.Add(new CondicionVehiculo { Descripcion = descripcion });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── ESTADOS DE VEHÍCULO ─────────────────────────────────────

        [HttpGet("estados-vehiculo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEstados() =>
            Ok(await _context.EstadosVehiculo.ToListAsync());

        [HttpPost("estados-vehiculo")]
        public async Task<IActionResult> PostEstado([FromBody] CrearEstadoRequest request)
        {
            _context.EstadosVehiculo.Add(new EstadoVehiculo
            {
                Descripcion = request.Descripcion,
                PermiteVenta = request.PermiteVenta,
                VisibleEnCatalogo = request.VisibleEnCatalogo
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── ORÍGENES DE VEHÍCULO ────────────────────────────────────

        [HttpGet("origenes-vehiculo")]
        public async Task<IActionResult> GetOrigenes() =>
            Ok(await _context.OrigenesVehiculo.ToListAsync());

        [HttpPost("origenes-vehiculo")]
        public async Task<IActionResult> PostOrigen([FromBody] string descripcion)
        {
            _context.OrigenesVehiculo.Add(new OrigenVehiculo { Descripcion = descripcion });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── TIPOS DE GASTO ──────────────────────────────────────────

        [HttpGet("tipos-gasto")]
        public async Task<IActionResult> GetTiposGasto() =>
            Ok(await _context.TiposGasto.Where(t => t.Activo).ToListAsync());

        [HttpPost("tipos-gasto")]
        public async Task<IActionResult> PostTipoGasto([FromBody] string descripcion)
        {
            _context.TiposGasto.Add(new TipoGasto { Descripcion = descripcion });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── FORMAS DE PAGO ──────────────────────────────────────────

        [HttpGet("formas-pago")]
        public async Task<IActionResult> GetFormasPago() =>
            Ok(await _context.FormasPago.ToListAsync());

        [HttpPost("formas-pago")]
        public async Task<IActionResult> PostFormaPago([FromBody] CrearFormaPagoRequest request)
        {
            _context.FormasPago.Add(new FormaPago
            {
                Descripcion = request.Descripcion,
                GeneraNotaCredito = request.GeneraNotaCredito
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── TIPOS DE VENTA ──────────────────────────────────────────

        [HttpGet("tipos-venta")]
        public async Task<IActionResult> GetTiposVenta() =>
            Ok(await _context.TiposVenta.ToListAsync());

        [HttpPost("tipos-venta")]
        public async Task<IActionResult> PostTipoVenta([FromBody] CrearTipoVentaRequest request)
        {
            _context.TiposVenta.Add(new TipoVenta
            {
                Descripcion = request.Descripcion,
                RequiereFinanciacion = request.RequiereFinanciacion
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── ESTADOS DE CITA ─────────────────────────────────────────

        [HttpGet("estados-cita")]
        public async Task<IActionResult> GetEstadosCita() =>
            Ok(await _context.EstadosCita.ToListAsync());

        [HttpPost("estados-cita")]
        public async Task<IActionResult> PostEstadoCita([FromBody] CrearEstadoCitaRequest request)
        {
            _context.EstadosCita.Add(new EstadoCita
            {
                Descripcion = request.Descripcion,
                Color = request.Color
            });
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    // ─── REQUEST CLASSES ─────────────────────────────────────────────

    public class CrearEstadoRequest
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool PermiteVenta { get; set; } = true;
        public bool VisibleEnCatalogo { get; set; } = true;
    }

    public class CrearFormaPagoRequest
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool GeneraNotaCredito { get; set; } = false;
    }

    public class CrearTipoVentaRequest
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool RequiereFinanciacion { get; set; } = false;
    }

    public class CrearEstadoCitaRequest
    {
        public string Descripcion { get; set; } = string.Empty;
        public string? Color { get; set; }
    }
}