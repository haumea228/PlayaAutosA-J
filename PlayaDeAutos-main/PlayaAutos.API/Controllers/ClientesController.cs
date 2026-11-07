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
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetClientes()
        {
            return await _context.Clientes
                .Where(c => c.Activo)
                .Select(c => new ClienteDto
                {
                    ClienteId = c.ClienteId,
                    Nombre = c.Nombre,
                    CI_RUC = c.CI_RUC,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    Direccion = c.Direccion,
                    Activo = c.Activo
                })
                .ToListAsync();
        }

        // GET: api/clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDto>> GetCliente(int id)
        {
            var c = await _context.Clientes
                .Include(x => x.Consignante)
                .FirstOrDefaultAsync(x => x.ClienteId == id);
            if (c == null) return NotFound();

            return new ClienteDto
            {
                ClienteId = c.ClienteId,
                Nombre = c.Nombre,
                CI_RUC = c.CI_RUC,
                Telefono = c.Telefono,
                Email = c.Email,
                Direccion = c.Direccion,
                Activo = c.Activo,
                EsConsignante = c.Consignante != null,
                ConsignanteId = c.Consignante?.ConsignanteId,
                PorcentajeComisionDefault = c.Consignante?.PorcentajeComisionDefault
            };
        }

        // PUT: api/clientes/5/convertir-consignante
        [HttpPut("{id}/convertir-consignante")]
        public async Task<IActionResult> ConvertirConsignante(int id, ConvertirConsignanteDto dto)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Consignante)
                .FirstOrDefaultAsync(c => c.ClienteId == id);

            if (cliente == null) return NotFound();
            if (cliente.Consignante != null)
                return BadRequest("El cliente ya es consignante.");

            var consignante = new Consignante
            {
                ClienteId = id,
                PorcentajeComisionDefault = dto.PorcentajeComisionDefault,
                Activo = true,
                FechaAlta = DateTime.Now
            };
            _context.Consignantes.Add(consignante);
            await _context.SaveChangesAsync();

            return Ok(new { consignanteId = consignante.ConsignanteId });
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<ClienteDto>> PostCliente(CrearClienteDto dto)
        {
            // Verificar CI/RUC duplicado
            if (await _context.Clientes.AnyAsync(c => c.CI_RUC == dto.CI_RUC))
                return BadRequest("Ya existe un cliente con ese CI/RUC.");

            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                CI_RUC = dto.CI_RUC,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Direccion = dto.Direccion,
                RangoPrecioMin = dto.RangoPrecioMin,
                RangoPrecioMax = dto.RangoPrecioMax,
                FechaAlta = DateTime.Now
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.ClienteId }, new ClienteDto
            {
                ClienteId = cliente.ClienteId,
                Nombre = cliente.Nombre,
                CI_RUC = cliente.CI_RUC,
                Telefono = cliente.Telefono,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                Activo = cliente.Activo
            });
        }

        // PUT: api/clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, CrearClienteDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            cliente.Nombre = dto.Nombre;
            cliente.CI_RUC = dto.CI_RUC;
            cliente.Telefono = dto.Telefono;
            cliente.Email = dto.Email;
            cliente.Direccion = dto.Direccion;
            cliente.RangoPrecioMin = dto.RangoPrecioMin;
            cliente.RangoPrecioMax = dto.RangoPrecioMax;
            cliente.Activo = dto.Activo;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/clientes/5 (baja lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            cliente.Activo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}