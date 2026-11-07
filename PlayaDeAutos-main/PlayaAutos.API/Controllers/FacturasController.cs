using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Models;
using PlayaAutos.API.Services;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor,Cajero")]
    public class FacturasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FacturasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/facturas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FacturaDto>>> GetFacturas()
        {
            return await _context.Facturas
                .Include(f => f.Venta).ThenInclude(v => v.Cliente)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(f => f.Timbrado)
                .Select(f => new FacturaDto
                {
                    FacturaId = f.FacturaId,
                    NumeroFactura = f.NumeroFactura,
                    Timbrado = f.Timbrado.NumeroTimbrado,
                    FechaEmision = f.FechaEmision,
                    Cliente = f.Venta.Cliente.Nombre,
                    Vehiculo = f.Venta.Vehiculo.Modelo.Marca.Nombre + " " +
                               f.Venta.Vehiculo.Modelo.Nombre,
                    Subtotal = f.Subtotal,
                    IVA = f.IVA,
                    Total = f.Total,
                    RutaPDF = f.RutaPDF
                })
                .ToListAsync();
        }

        // GET: api/facturas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FacturaDto>> GetFactura(int id)
        {
            var f = await _context.Facturas
                .Include(f => f.Venta).ThenInclude(v => v.Cliente)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(f => f.Timbrado)
                .FirstOrDefaultAsync(f => f.FacturaId == id);

            if (f == null) return NotFound();

            return new FacturaDto
            {
                FacturaId = f.FacturaId,
                NumeroFactura = f.NumeroFactura,
                Timbrado = f.Timbrado.NumeroTimbrado,
                FechaEmision = f.FechaEmision,
                Cliente = f.Venta.Cliente.Nombre,
                Vehiculo = f.Venta.Vehiculo.Modelo.Marca.Nombre + " " +
                           f.Venta.Vehiculo.Modelo.Nombre,
                Subtotal = f.Subtotal,
                IVA = f.IVA,
                Total = f.Total,
                RutaPDF = f.RutaPDF
            };
        }

        // GET: api/facturas/venta/5
        [HttpGet("venta/{ventaId}")]
        public async Task<ActionResult<FacturaDto>> GetFacturaPorVenta(int ventaId)
        {
            var f = await _context.Facturas
                .Include(f => f.Venta).ThenInclude(v => v.Cliente)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(f => f.Timbrado)
                .FirstOrDefaultAsync(f => f.VentaId == ventaId);

            if (f == null) return NotFound();

            return new FacturaDto
            {
                FacturaId = f.FacturaId,
                NumeroFactura = f.NumeroFactura,
                Timbrado = f.Timbrado.NumeroTimbrado,
                FechaEmision = f.FechaEmision,
                Cliente = f.Venta.Cliente.Nombre,
                Vehiculo = f.Venta.Vehiculo.Modelo.Marca.Nombre + " " +
                           f.Venta.Vehiculo.Modelo.Nombre,
                Subtotal = f.Subtotal,
                IVA = f.IVA,
                Total = f.Total,
                RutaPDF = f.RutaPDF
            };
        }

        // GET: api/facturas/{id}/pdf
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            var f = await _context.Facturas
                .Include(f => f.Timbrado)
                .Include(f => f.Venta).ThenInclude(v => v.Cliente)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Tipo)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Condicion)
                .Include(f => f.Venta).ThenInclude(v => v.PagosVenta).ThenInclude(p => p.FormaPago)
                .FirstOrDefaultAsync(f => f.FacturaId == id);

            if (f == null) return NotFound();

            var datos = new FacturaPdfDataDto
            {
                NumeroFactura          = f.NumeroFactura,
                FechaEmision           = f.FechaEmision,
                NumeroTimbrado         = f.Timbrado.NumeroTimbrado,
                TimbradoVigenciaDesde  = f.Timbrado.FechaInicio,
                TimbradoVigenciaHasta  = f.Timbrado.FechaVencimiento,
                ClienteNombre          = f.Venta.Cliente.Nombre,
                ClienteRUC             = f.Venta.Cliente.CI_RUC,
                ClienteDireccion       = f.Venta.Cliente.Direccion,
                ClienteTelefono        = f.Venta.Cliente.Telefono,
                VehiculoMarca          = f.Venta.Vehiculo.Modelo.Marca.Nombre,
                VehiculoModelo         = f.Venta.Vehiculo.Modelo.Nombre,
                VehiculoTipo           = f.Venta.Vehiculo.Tipo.Descripcion,
                VehiculoCondicion      = f.Venta.Vehiculo.Condicion.Descripcion,
                VehiculoAnio           = f.Venta.Vehiculo.Anio,
                VehiculoColor          = f.Venta.Vehiculo.Color,
                VehiculoDescripcion    = f.Venta.Vehiculo.Descripcion,
                FormaPago              = string.Join(", ", f.Venta.PagosVenta.Select(p => p.FormaPago.Descripcion)),
                Subtotal               = f.Subtotal,
                IVA                    = f.IVA,
                Total                  = f.Total
            };

            var pdfBytes = FacturaPdfService.Generar(datos);
            var nombreArchivo = $"Factura-{f.NumeroFactura.Replace("/", "-").Replace("\\", "-")}.pdf";

            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        // POST: api/facturas
        [HttpPost]
        public async Task<IActionResult> PostFactura(CrearFacturaDto dto)
        {
            // Verificar que la venta existe
            if (!await _context.Ventas.AnyAsync(v => v.VentaId == dto.VentaId))
                return BadRequest("La venta no existe.");

            // Verificar que no tenga ya una factura
            if (await _context.Facturas.AnyAsync(f => f.VentaId == dto.VentaId))
                return BadRequest("Esta venta ya tiene una factura generada.");

            // Obtener timbrado activo
            var timbrado = await _context.Timbrados
                .FirstOrDefaultAsync(t => t.TimbradoId == dto.TimbradoId && t.Activo);

            if (timbrado == null)
                return BadRequest("El timbrado especificado no existe o no está activo.");

            if (timbrado.UltimoNumeroUsado >= timbrado.NumeroHasta)
                return BadRequest("El timbrado ha alcanzado el límite de numeración.");

            // Generar número de factura
            timbrado.UltimoNumeroUsado++;
            var numeroFactura = $"{timbrado.NumeroTimbrado}-{timbrado.UltimoNumeroUsado:D8}";

            var factura = new Factura
            {
                VentaId = dto.VentaId,
                TimbradoId = dto.TimbradoId,
                NumeroFactura = numeroFactura,
                FechaEmision = dto.FechaEmision,
                Subtotal = dto.Subtotal,
                IVA = dto.IVA,
                Total = dto.Total,
                FechaGeneracion = DateTime.Now,
                UsuarioGeneracion = 1 // temporal hasta JWT completo
            };

            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFactura), new { id = factura.FacturaId },
                new { factura.FacturaId, factura.NumeroFactura });
        }
    }
}