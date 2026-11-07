using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Services;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor,Cajero")]
    public class NotasCreditoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotasCreditoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notascredito
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotaCreditoDto>>> GetNotasCredito()
        {
            return await _context.NotasCredito
                .Include(n => n.Venta).ThenInclude(v => v.Cliente)
                .Include(n => n.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(n => n.Timbrado)
                .Include(n => n.TasacionVehiculo)
                .OrderByDescending(n => n.FechaEmision)
                .Select(n => new NotaCreditoDto
                {
                    NotaCreditoId       = n.NotaCreditoId,
                    VentaId             = n.VentaId,
                    NumeroNota          = n.NumeroNota,
                    Timbrado            = n.Timbrado.NumeroTimbrado,
                    NumeroTimbrado      = n.Timbrado.NumeroTimbrado,
                    TimbradoVigenciaDesde = n.Timbrado.FechaInicio,
                    TimbradoVigenciaHasta = n.Timbrado.FechaVencimiento,
                    FechaEmision        = n.FechaEmision,
                    Cliente             = n.Venta.Cliente.Nombre,
                    ClienteRUC          = n.Venta.Cliente.CI_RUC,
                    ClienteDireccion    = n.Venta.Cliente.Direccion,
                    ClienteTelefono     = n.Venta.Cliente.Telefono,
                    VehiculoVenta       = n.Venta.Vehiculo.Modelo.Marca.Nombre + " " +
                                         n.Venta.Vehiculo.Modelo.Nombre,
                    VehiculoPermuta     = n.TasacionVehiculo != null
                        ? n.TasacionVehiculo.MarcaVehiculo + " " +
                          n.TasacionVehiculo.ModeloVehiculo + " " +
                          n.TasacionVehiculo.AnhoVehiculo
                        : null,
                    Motivo              = n.Motivo,
                    Monto               = n.Monto,
                    TasacionVehiculoId  = n.TasacionVehiculoId
                })
                .ToListAsync();
        }

        // GET: api/notascredito/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NotaCreditoDto>> GetNotaCredito(int id)
        {
            var n = await _context.NotasCredito
                .Include(n => n.Venta).ThenInclude(v => v.Cliente)
                .Include(n => n.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(n => n.Venta).ThenInclude(v => v.Factura)
                .Include(n => n.Timbrado)
                .Include(n => n.TasacionVehiculo)
                .FirstOrDefaultAsync(n => n.NotaCreditoId == id);

            if (n == null) return NotFound();

            return MapDto(n);
        }

        // GET: api/notascredito/venta/5
        [HttpGet("venta/{ventaId}")]
        public async Task<ActionResult<IEnumerable<NotaCreditoDto>>> GetNotasPorVenta(int ventaId)
        {
            var notas = await _context.NotasCredito
                .Include(n => n.Venta).ThenInclude(v => v.Cliente)
                .Include(n => n.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(n => n.Venta).ThenInclude(v => v.Factura)
                .Include(n => n.Timbrado)
                .Include(n => n.TasacionVehiculo)
                .Where(n => n.VentaId == ventaId)
                .ToListAsync();

            return notas.Select(MapDto).ToList();
        }

        // GET: api/notascredito/{id}/pdf
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            var n = await _context.NotasCredito
                .Include(n => n.Timbrado)
                .Include(n => n.Venta).ThenInclude(v => v.Cliente)
                .Include(n => n.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(n => n.Venta).ThenInclude(v => v.Factura)
                .Include(n => n.TasacionVehiculo)
                .FirstOrDefaultAsync(n => n.NotaCreditoId == id);

            if (n == null) return NotFound();

            var subtotal = (long)Math.Round(n.Monto / 1.10m);
            var iva      = n.Monto - subtotal;

            var datos = new NotaCreditoPdfDataDto
            {
                NumeroNota            = n.NumeroNota,
                FechaEmision          = n.FechaEmision,
                Motivo                = n.Motivo,
                NumeroTimbrado        = n.Timbrado.NumeroTimbrado,
                TimbradoVigenciaDesde = n.Timbrado.FechaInicio,
                TimbradoVigenciaHasta = n.Timbrado.FechaVencimiento,
                ClienteNombre         = n.Venta.Cliente.Nombre,
                ClienteRUC            = n.Venta.Cliente.CI_RUC,
                ClienteDireccion      = n.Venta.Cliente.Direccion,
                ClienteTelefono       = n.Venta.Cliente.Telefono,
                VehiculoMarca         = n.Venta.Vehiculo.Modelo.Marca.Nombre,
                VehiculoModelo        = n.Venta.Vehiculo.Modelo.Nombre,
                VehiculoAnio          = n.Venta.Vehiculo.Anio,
                VehiculoColor         = n.Venta.Vehiculo.Color,
                PermutaMarca          = n.TasacionVehiculo?.MarcaVehiculo,
                PermutaModelo         = n.TasacionVehiculo?.ModeloVehiculo,
                PermutaAnio           = n.TasacionVehiculo?.AnhoVehiculo,
                PermutaKilometraje    = n.TasacionVehiculo?.KilometrajeVehiculo,
                PermutaEstado         = n.TasacionVehiculo?.EstadoGeneralVehiculo,
                PermutaColor          = n.TasacionVehiculo?.ColorVehiculo,
                NumeroFactura         = n.Venta.Factura?.NumeroFactura ?? "",
                Monto                 = n.Monto,
                Subtotal              = subtotal,
                IVA                   = iva
            };

            var pdfBytes      = NotaCreditoPdfService.Generar(datos);
            var nombreArchivo = $"NotaCredito-{n.NumeroNota.Replace("/", "-").Replace("\\", "-")}.pdf";

            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        // ── Helper ──────────────────────────────────────────────────────────
        private static NotaCreditoDto MapDto(Models.NotaCredito n) => new()
        {
            NotaCreditoId         = n.NotaCreditoId,
            VentaId               = n.VentaId,
            NumeroNota            = n.NumeroNota,
            Timbrado              = n.Timbrado.NumeroTimbrado,
            NumeroTimbrado        = n.Timbrado.NumeroTimbrado,
            TimbradoVigenciaDesde = n.Timbrado.FechaInicio,
            TimbradoVigenciaHasta = n.Timbrado.FechaVencimiento,
            FechaEmision          = n.FechaEmision,
            Cliente               = n.Venta.Cliente.Nombre,
            ClienteRUC            = n.Venta.Cliente.CI_RUC,
            ClienteDireccion      = n.Venta.Cliente.Direccion,
            ClienteTelefono       = n.Venta.Cliente.Telefono,
            VehiculoVenta         = n.Venta.Vehiculo.Modelo.Marca.Nombre + " " +
                                    n.Venta.Vehiculo.Modelo.Nombre,
            VehiculoPermuta       = n.TasacionVehiculo != null
                ? n.TasacionVehiculo.MarcaVehiculo + " " +
                  n.TasacionVehiculo.ModeloVehiculo + " " +
                  n.TasacionVehiculo.AnhoVehiculo
                : null,
            Motivo                = n.Motivo,
            Monto                 = n.Monto,
            TasacionVehiculoId    = n.TasacionVehiculoId,
            NumeroFactura         = n.Venta.Factura?.NumeroFactura ?? ""
        };
    }
}