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
    [Authorize(Roles = "AdministradorP")]
    public class TimbradosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimbradosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/timbrados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimbradoDto>>> GetTimbrados()
        {
            return await _context.Timbrados
                .Select(t => new TimbradoDto
                {
                    TimbradoId = t.TimbradoId,
                    NumeroTimbrado = t.NumeroTimbrado,
                    FechaInicio = t.FechaInicio,
                    FechaVencimiento = t.FechaVencimiento,
                    NumeroDesde = t.NumeroDesde,
                    NumeroHasta = t.NumeroHasta,
                    UltimoNumeroUsado = t.UltimoNumeroUsado,
                    Activo = t.Activo
                })
                .ToListAsync();
        }

        // GET: api/timbrados/activo
        [HttpGet("activo")]
        [Authorize(Roles = "AdministradorP,Vendedor")]
        public async Task<ActionResult<TimbradoDto>> GetTimbradoActivo()
        {
            var t = await _context.Timbrados
                .Where(t => t.Activo &&
                            t.FechaInicio <= DateTime.Now &&
                            t.FechaVencimiento >= DateTime.Now &&
                            t.UltimoNumeroUsado < t.NumeroHasta)
                .FirstOrDefaultAsync();

            if (t == null)
                return NotFound("No hay timbrado activo disponible.");

            return new TimbradoDto
            {
                TimbradoId = t.TimbradoId,
                NumeroTimbrado = t.NumeroTimbrado,
                FechaInicio = t.FechaInicio,
                FechaVencimiento = t.FechaVencimiento,
                NumeroDesde = t.NumeroDesde,
                NumeroHasta = t.NumeroHasta,
                UltimoNumeroUsado = t.UltimoNumeroUsado,
                Activo = t.Activo
            };
        }

        // GET: api/timbrados/5
        [HttpGet("{id}")]
        [Authorize(Roles = "AdministradorP,Vendedor")]
        public async Task<ActionResult<TimbradoDto>> GetTimbrado(int id)
        {
            var t = await _context.Timbrados.FindAsync(id);
            if (t == null) return NotFound();

            return new TimbradoDto
            {
                TimbradoId = t.TimbradoId,
                NumeroTimbrado = t.NumeroTimbrado,
                FechaInicio = t.FechaInicio,
                FechaVencimiento = t.FechaVencimiento,
                NumeroDesde = t.NumeroDesde,
                NumeroHasta = t.NumeroHasta,
                UltimoNumeroUsado = t.UltimoNumeroUsado,
                Activo = t.Activo
            };
        }

        // POST: api/timbrados
        [HttpPost]
        public async Task<IActionResult> PostTimbrado(CrearTimbradoDto dto)
        {
            // Verificar que no haya otro timbrado activo con el mismo número
            if (await _context.Timbrados.AnyAsync(t => t.NumeroTimbrado == dto.NumeroTimbrado))
                return BadRequest("Ya existe un timbrado con ese número.");

            var timbrado = new Timbrado
            {
                NumeroTimbrado = dto.NumeroTimbrado,
                FechaInicio = dto.FechaInicio,
                FechaVencimiento = dto.FechaVencimiento,
                NumeroDesde = dto.NumeroDesde,
                NumeroHasta = dto.NumeroHasta,
                UltimoNumeroUsado = dto.NumeroDesde - 1,
                Activo = true,
                FechaAlta = DateTime.Now
            };

            _context.Timbrados.Add(timbrado);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTimbrado), new { id = timbrado.TimbradoId }, new { timbrado.TimbradoId });
        }

        // PUT: api/timbrados/5/desactivar
        [HttpPut("{id}/desactivar")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var timbrado = await _context.Timbrados.FindAsync(id);
            if (timbrado == null) return NotFound();

            timbrado.Activo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}