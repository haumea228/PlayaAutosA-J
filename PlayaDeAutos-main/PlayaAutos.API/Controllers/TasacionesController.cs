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
    [Authorize(Roles = "AdministradorP,Vendedor")]
    public class TasacionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasacionesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tasaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TasacionDto>>> GetTasaciones()
        {
            var list = await _context.TasacionesVehiculo.Include(t => t.Cliente).ToListAsync();
            return list.Select(MapDto).ToList();
        }

        // GET: api/tasaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TasacionDto>> GetTasacion(int id)
        {
            var t = await _context.TasacionesVehiculo
                .Include(t => t.Cliente)
                .FirstOrDefaultAsync(t => t.TasacionVehiculoId == id);

            if (t == null) return NotFound();
            return MapDto(t);
        }

        // GET: api/tasaciones/pendientes
        [HttpGet("pendientes")]
        public async Task<ActionResult<IEnumerable<TasacionDto>>> GetPendientes()
        {
            var list = await _context.TasacionesVehiculo
                .Include(t => t.Cliente)
                .Where(t => t.EstadoTasacion == "Pendiente")
                .ToListAsync();
            return list.Select(MapDto).ToList();
        }

        // GET: api/tasaciones/aprobadas/cliente/5
        [HttpGet("aprobadas/cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<TasacionDto>>> GetAprobadasPorCliente(int clienteId)
        {
            var list = await _context.TasacionesVehiculo
                .Include(t => t.Cliente)
                .Where(t => t.EstadoTasacion == "Aprobada" && t.ClienteId == clienteId)
                .ToListAsync();
            return list.Select(MapDto).ToList();
        }

        // POST: api/tasaciones
        [HttpPost]
        public async Task<ActionResult<TasacionDto>> PostTasacion(CrearTasacionDto dto)
        {
            if (!await _context.Clientes.AnyAsync(c => c.ClienteId == dto.ClienteId))
                return BadRequest("El cliente no existe.");

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var tasacion = new TasacionVehiculo
            {
                ClienteId = dto.ClienteId,
                MarcaVehiculo = dto.MarcaVehiculo,
                ModeloVehiculo = dto.ModeloVehiculo,
                AnhoVehiculo = dto.AnhoVehiculo,
                KilometrajeVehiculo = dto.KilometrajeVehiculo,
                EstadoGeneralVehiculo = dto.EstadoGeneralVehiculo,
                ColorVehiculo = dto.ColorVehiculo,
                ValorTasacion = dto.ValorTasacion,
                PrecioVenta = dto.PrecioVenta,
                ModeloId = dto.ModeloId,
                TipoId = dto.TipoId,
                CondicionId = dto.CondicionId,
                OrigenId = dto.OrigenId,
                EstadoTasacion = "Pendiente",
                UsuarioTasacion = usuarioId,
                FechaTasacion = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            _context.TasacionesVehiculo.Add(tasacion);
            await _context.SaveChangesAsync();

            await _context.Entry(tasacion).Reference(t => t.Cliente).LoadAsync();
            return CreatedAtAction(nameof(GetTasacion), new { id = tasacion.TasacionVehiculoId }, MapDto(tasacion));
        }

        // PUT: api/tasaciones/5/aprobar
        [HttpPut("{id}/aprobar")]
        public async Task<IActionResult> AprobarTasacion(int id, AprobarTasacionDto dto)
        {
            var tasacion = await _context.TasacionesVehiculo.FindAsync(id);
            if (tasacion == null) return NotFound();

            if (tasacion.EstadoTasacion != "Pendiente")
                return BadRequest($"Solo se pueden aprobar tasaciones en estado Pendiente. Estado actual: {tasacion.EstadoTasacion}");

            tasacion.ValorTasacion = dto.ValorTasacionFinal;
            tasacion.PrecioVenta = dto.PrecioVentaFinal;
            // Mantiene los valores del catálogo si el form de aprobar no los cambia
            if (dto.ModeloId > 0) tasacion.ModeloId = dto.ModeloId;
            if (dto.TipoId > 0) tasacion.TipoId = dto.TipoId;
            if (dto.CondicionId > 0) tasacion.CondicionId = dto.CondicionId;
            if (dto.OrigenId > 0) tasacion.OrigenId = dto.OrigenId;
            tasacion.ColorVehiculo = dto.Color ?? tasacion.ColorVehiculo;
            if (dto.Kilometraje > 0) tasacion.KilometrajeVehiculo = dto.Kilometraje;
            tasacion.EstadoTasacion = "Aprobada";

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/tasaciones/5/rechazar
        [HttpPut("{id}/rechazar")]
        public async Task<IActionResult> RechazarTasacion(int id)
        {
            var tasacion = await _context.TasacionesVehiculo.FindAsync(id);
            if (tasacion == null) return NotFound();

            if (tasacion.EstadoTasacion != "Pendiente")
                return BadRequest($"Solo se pueden rechazar tasaciones en estado Pendiente. Estado actual: {tasacion.EstadoTasacion}");

            tasacion.EstadoTasacion = "Rechazada";
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static TasacionDto MapDto(TasacionVehiculo t) => new()
        {
            TasacionVehiculoId = t.TasacionVehiculoId,
            ClienteId = t.ClienteId,
            Cliente = t.Cliente?.Nombre ?? "",
            MarcaVehiculo = t.MarcaVehiculo,
            ModeloVehiculo = t.ModeloVehiculo,
            AnhoVehiculo = t.AnhoVehiculo,
            KilometrajeVehiculo = t.KilometrajeVehiculo,
            EstadoGeneralVehiculo = t.EstadoGeneralVehiculo,
            ColorVehiculo = t.ColorVehiculo,
            ValorTasacion = t.ValorTasacion,
            PrecioVenta = t.PrecioVenta,
            EstadoTasacion = t.EstadoTasacion,
            FechaTasacion = t.FechaTasacion,
            ModeloId = t.ModeloId,
            TipoId = t.TipoId,
            CondicionId = t.CondicionId,
            OrigenId = t.OrigenId
        };
    }
}