using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.Models;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor")]
    public class ModelosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ModelosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/modelos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetModelos()
        {
            return await _context.Modelos
                .Include(m => m.Marca)
                .Where(m => m.Activo)
                .Select(m => new
                {
                    m.ModeloId,
                    m.Nombre,
                    m.MarcaId,
                    Marca = m.Marca.Nombre,
                    m.Activo
                })
                .ToListAsync();
        }

        // GET: api/modelos/marca/5
        [HttpGet("marca/{marcaId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetModelosPorMarca(int marcaId)
        {
            return await _context.Modelos
                .Where(m => m.MarcaId == marcaId && m.Activo)
                .Select(m => new
                {
                    m.ModeloId,
                    m.Nombre,
                    m.MarcaId,
                    m.Activo
                })
                .ToListAsync();
        }

        // POST: api/modelos
        [HttpPost]
        public async Task<IActionResult> PostModelo([FromBody] CrearModeloRequest request)
        {
            if (!await _context.Marcas.AnyAsync(m => m.MarcaId == request.MarcaId))
                return BadRequest("La marca especificada no existe.");

            var modelo = new Modelo
            {
                Nombre = request.Nombre,
                MarcaId = request.MarcaId
            };

            _context.Modelos.Add(modelo);
            await _context.SaveChangesAsync();
            return Ok(new { modelo.ModeloId, modelo.Nombre, modelo.MarcaId });
        }

        // DELETE: api/modelos/5 (baja lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModelo(int id)
        {
            var modelo = await _context.Modelos.FindAsync(id);
            if (modelo == null) return NotFound();
            modelo.Activo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CrearModeloRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public int MarcaId { get; set; }
    }
}
