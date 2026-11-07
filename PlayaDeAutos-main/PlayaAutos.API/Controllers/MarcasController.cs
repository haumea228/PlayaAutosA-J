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
    [Authorize(Roles = "AdministradorP,Vendedor")]
    public class MarcasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MarcasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/marcas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MarcaDto>>> GetMarcas()
        {
            return await _context.Marcas
                .Where(m => m.Activo)
                .Select(m => new MarcaDto
                {
                    MarcaId = m.MarcaId,
                    Nombre = m.Nombre,
                    Activo = m.Activo
                })
                .ToListAsync();
        }

        // GET: api/marcas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MarcaDto>> GetMarca(int id)
        {
            var marca = await _context.Marcas.FindAsync(id);
            if (marca == null) return NotFound();

            return new MarcaDto
            {
                MarcaId = marca.MarcaId,
                Nombre = marca.Nombre,
                Activo = marca.Activo
            };
        }

        // POST: api/marcas
        [HttpPost]
        public async Task<ActionResult<MarcaDto>> PostMarca(CrearMarcaDto dto)
        {
            var marca = new Marca
            {
                Nombre = dto.Nombre,
                FechaAlta = DateTime.Now
            };

            _context.Marcas.Add(marca);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMarca), new { id = marca.MarcaId }, new MarcaDto
            {
                MarcaId = marca.MarcaId,
                Nombre = marca.Nombre,
                Activo = marca.Activo
            });
        }

        // PUT: api/marcas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMarca(int id, CrearMarcaDto dto)
        {
            var marca = await _context.Marcas.FindAsync(id);
            if (marca == null) return NotFound();

            marca.Nombre = dto.Nombre;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/marcas/5 (baja lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMarca(int id)
        {
            var marca = await _context.Marcas.FindAsync(id);
            if (marca == null) return NotFound();

            marca.Activo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}