using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Helpers;
using PlayaAutos.API.Models;
using System.Security.Claims;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor")]
    public class VehiculosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiculosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/vehiculos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetVehiculos()
        {
            return await _context.Vehiculos
                .Include(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(v => v.Tipo)
                .Include(v => v.Condicion)
                .Include(v => v.Estado)
                .Include(v => v.Fotos)
                .Select(v => new VehiculoDto
                {
                    VehiculoId = v.VehiculoId,
                    CodigoInterno = v.CodigoInterno,
                    MarcaId = v.Modelo.MarcaId,
                    ModeloId = v.ModeloId,
                    TipoId = v.TipoId,
                    CondicionId = v.CondicionId,
                    EstadoId = v.EstadoId,
                    OrigenId = v.OrigenId,
                    Marca = v.Modelo.Marca.Nombre,
                    Modelo = v.Modelo.Nombre,
                    Tipo = v.Tipo.Descripcion,
                    Condicion = v.Condicion.Descripcion,
                    Estado = v.Estado.Descripcion,
                    Anio = v.Anio,
                    Color = v.Color,
                    Kilometraje = v.Kilometraje,
                    PrecioVenta = v.PrecioVenta,
                    CostoAdquisicion = v.CostoAdquisicion,
                    Descripcion = v.Descripcion,
                    Transmision = v.Transmision,
                    Combustible = v.Combustible,
                    CantidadPuertas = v.CantidadPuertas,
                    Motor = v.Motor,
                    Traccion = v.Traccion,
                    FotoPrincipal = v.Fotos
                        .Where(f => f.EsPrincipal)
                        .Select(f => f.URL)
                        .FirstOrDefault(),
                    Fotos = v.Fotos
                        .OrderBy(f => f.Orden)
                        .Select(f => new FotoVehiculoDto
                        {
                            FotoId = f.FotoId,
                            URL = f.URL,
                            NombreArchivo = f.NombreArchivo,
                            EsPrincipal = f.EsPrincipal,
                            Orden = f.Orden
                        }).ToList()
                })
                .ToListAsync();
        }

        // GET: api/vehiculos/catalogo
        [AllowAnonymous]
        [HttpGet("catalogo")]
        public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetCatalogo()
        {
            return await _context.Vehiculos
                .Include(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(v => v.Tipo)
                .Include(v => v.Condicion)
                .Include(v => v.Estado)
                .Include(v => v.Fotos)
                .Where(v => v.Estado.VisibleEnCatalogo)
                .Select(v => new VehiculoDto
                {
                    VehiculoId = v.VehiculoId,
                    CodigoInterno = v.CodigoInterno,
                    Marca = v.Modelo.Marca.Nombre,
                    Modelo = v.Modelo.Nombre,
                    Tipo = v.Tipo.Descripcion,
                    Condicion = v.Condicion.Descripcion,
                    Estado = v.Estado.Descripcion,
                    Anio = v.Anio,
                    Color = v.Color,
                    Kilometraje = v.Kilometraje,
                    PrecioVenta = v.PrecioVenta,
                    // Catálogo público: ficha técnica + descripción (la app la
                    // muestra sin cambios en su pantalla de detalle).
                    Descripcion = FichaTecnica.ArmarDescripcion(v.Descripcion,
                        v.Transmision, v.Combustible, v.CantidadPuertas, v.Motor, v.Traccion),
                    Transmision = v.Transmision,
                    Combustible = v.Combustible,
                    CantidadPuertas = v.CantidadPuertas,
                    Motor = v.Motor,
                    Traccion = v.Traccion,
                    FotoPrincipal = v.Fotos
                        .Where(f => f.EsPrincipal)
                        .Select(f => f.URL)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        // GET: api/vehiculos/catalogo/{id}  — público, sin token
        [AllowAnonymous]
        [HttpGet("catalogo/{id}")]
        public async Task<ActionResult<VehiculoDto>> GetCatalogoDetalle(int id)
        {
            var v = await _context.Vehiculos
                .Include(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(v => v.Tipo)
                .Include(v => v.Condicion)
                .Include(v => v.Estado)
                .Include(v => v.Fotos)
                .FirstOrDefaultAsync(v => v.VehiculoId == id && v.Estado.VisibleEnCatalogo);

            if (v == null) return NotFound();

            return new VehiculoDto
            {
                VehiculoId = v.VehiculoId,
                Marca = v.Modelo.Marca.Nombre,
                Modelo = v.Modelo.Nombre,
                Tipo = v.Tipo.Descripcion,
                Condicion = v.Condicion.Descripcion,
                Estado = v.Estado.Descripcion,
                Anio = v.Anio,
                Color = v.Color,
                Kilometraje = v.Kilometraje,
                PrecioVenta = v.PrecioVenta,
                Descripcion = FichaTecnica.ArmarDescripcion(v.Descripcion,
                    v.Transmision, v.Combustible, v.CantidadPuertas, v.Motor, v.Traccion),
                Transmision = v.Transmision,
                Combustible = v.Combustible,
                CantidadPuertas = v.CantidadPuertas,
                Motor = v.Motor,
                Traccion = v.Traccion,
                FotoPrincipal = v.Fotos.Where(f => f.EsPrincipal).Select(f => f.URL).FirstOrDefault(),
                Fotos = v.Fotos.OrderBy(f => f.Orden).Select(f => new FotoVehiculoDto
                {
                    FotoId = f.FotoId,
                    URL = f.URL,
                    NombreArchivo = f.NombreArchivo,
                    EsPrincipal = f.EsPrincipal,
                    Orden = f.Orden
                }).ToList()
            };
        }

        // GET: api/vehiculos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehiculoDto>> GetVehiculo(int id)
        {
            var v = await _context.Vehiculos
                .Include(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(v => v.Tipo)
                .Include(v => v.Condicion)
                .Include(v => v.Estado)
                .Include(v => v.Fotos)
                .FirstOrDefaultAsync(v => v.VehiculoId == id);

            if (v == null) return NotFound();

            return new VehiculoDto
            {
                VehiculoId = v.VehiculoId,
                CodigoInterno = v.CodigoInterno,
                MarcaId = v.Modelo.MarcaId,
                ModeloId = v.ModeloId,
                TipoId = v.TipoId,
                CondicionId = v.CondicionId,
                EstadoId = v.EstadoId,
                OrigenId = v.OrigenId,
                Marca = v.Modelo.Marca.Nombre,
                Modelo = v.Modelo.Nombre,
                Tipo = v.Tipo.Descripcion,
                Condicion = v.Condicion.Descripcion,
                Estado = v.Estado.Descripcion,
                Anio = v.Anio,
                Color = v.Color,
                Kilometraje = v.Kilometraje,
                PrecioVenta = v.PrecioVenta,
                CostoAdquisicion = v.CostoAdquisicion,
                Descripcion = v.Descripcion,
                Transmision = v.Transmision,
                Combustible = v.Combustible,
                CantidadPuertas = v.CantidadPuertas,
                Motor = v.Motor,
                Traccion = v.Traccion,
                FotoPrincipal = v.Fotos.Where(f => f.EsPrincipal).Select(f => f.URL).FirstOrDefault(),
                Fotos = v.Fotos.OrderBy(f => f.Orden).Select(f => new FotoVehiculoDto
                {
                    FotoId = f.FotoId,
                    URL = f.URL,
                    NombreArchivo = f.NombreArchivo,
                    EsPrincipal = f.EsPrincipal,
                    Orden = f.Orden
                }).ToList()
            };
        }

        // POST: api/vehiculos
        [HttpPost]
        public async Task<ActionResult<VehiculoDto>> PostVehiculo(CrearVehiculoDto dto)
        {
            var errorFicha = ValidarFicha(dto);
            if (errorFicha != null) return BadRequest(errorFicha);

            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            var vehiculo = new Vehiculo
            {
                CodigoInterno = dto.CodigoInterno,
                ModeloId = dto.ModeloId,
                TipoId = dto.TipoId,
                CondicionId = dto.CondicionId,
                EstadoId = dto.EstadoId,
                OrigenId = dto.OrigenId,
                Anio = dto.Anio,
                Color = dto.Color,
                Kilometraje = dto.Kilometraje,
                PrecioVenta = dto.PrecioVenta,
                CostoAdquisicion = dto.CostoAdquisicion,
                Descripcion = dto.Descripcion,
                Transmision = dto.Transmision,
                Combustible = dto.Combustible,
                CantidadPuertas = dto.CantidadPuertas,
                Motor = dto.Motor,
                Traccion = dto.Traccion,
                FechaAlta = DateTime.Now,
                UsuarioAlta = userId
            };

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVehiculo), new { id = vehiculo.VehiculoId }, null);
        }

        // PUT: api/vehiculos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehiculo(int id, CrearVehiculoDto dto)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null) return NotFound();

            var errorFicha = ValidarFicha(dto);
            if (errorFicha != null) return BadRequest(errorFicha);

            vehiculo.CodigoInterno = dto.CodigoInterno;
            vehiculo.ModeloId = dto.ModeloId;
            vehiculo.TipoId = dto.TipoId;
            vehiculo.CondicionId = dto.CondicionId;
            vehiculo.EstadoId = dto.EstadoId;
            vehiculo.OrigenId = dto.OrigenId;
            vehiculo.Anio = dto.Anio;
            vehiculo.Color = dto.Color;
            vehiculo.Kilometraje = dto.Kilometraje;
            vehiculo.PrecioVenta = dto.PrecioVenta;
            vehiculo.CostoAdquisicion = dto.CostoAdquisicion;
            vehiculo.Descripcion = dto.Descripcion;
            vehiculo.Transmision = dto.Transmision;
            vehiculo.Combustible = dto.Combustible;
            vehiculo.CantidadPuertas = dto.CantidadPuertas;
            vehiculo.Motor = dto.Motor;
            vehiculo.Traccion = dto.Traccion;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/vehiculos/ficha-tecnica/opciones — valores permitidos para los selects de la web
        [HttpGet("ficha-tecnica/opciones")]
        public IActionResult GetOpcionesFichaTecnica() => Ok(new
        {
            Transmisiones = FichaTecnica.Transmisiones,
            Combustibles = FichaTecnica.Combustibles,
            Tracciones = FichaTecnica.Tracciones,
            FichaTecnica.PuertasMin,
            FichaTecnica.PuertasMax
        });

        /// <summary>Valida y normaliza (in place) los atributos de ficha técnica del DTO.</summary>
        private static string? ValidarFicha(CrearVehiculoDto dto)
        {
            var transmision = dto.Transmision;
            var combustible = dto.Combustible;
            var traccion = dto.Traccion;
            var motor = dto.Motor;

            var error = FichaTecnica.Validar(ref transmision, ref combustible, ref traccion, dto.CantidadPuertas, ref motor);
            if (error != null) return error;

            dto.Transmision = transmision;
            dto.Combustible = combustible;
            dto.Traccion = traccion;
            dto.Motor = motor;
            return null;
        }

        // DELETE: api/vehiculos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehiculo(int id)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Fotos)
                .FirstOrDefaultAsync(v => v.VehiculoId == id);

            if (vehiculo == null) return NotFound();

            _context.FotosVehiculo.RemoveRange(vehiculo.Fotos);
            _context.Vehiculos.Remove(vehiculo);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/vehiculos/{id}/fotos
        [HttpPost("{id}/fotos")]
        public async Task<IActionResult> PostFoto(int id, GuardarFotoDto dto)
        {
            if (!await _context.Vehiculos.AnyAsync(v => v.VehiculoId == id))
                return NotFound();

            // Si es principal, quitar la marca de las demás
            if (dto.EsPrincipal)
            {
                var fotosActuales = await _context.FotosVehiculo
                    .Where(f => f.VehiculoId == id && f.EsPrincipal)
                    .ToListAsync();
                fotosActuales.ForEach(f => f.EsPrincipal = false);
            }

            _context.FotosVehiculo.Add(new FotoVehiculo
            {
                VehiculoId = id,
                URL = dto.URL,
                NombreArchivo = dto.NombreArchivo,
                EsPrincipal = dto.EsPrincipal,
                Orden = dto.Orden,
                FechaAlta = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/vehiculos/{id}/fotos/{fotoId}
        [HttpDelete("{id}/fotos/{fotoId}")]
        public async Task<IActionResult> DeleteFoto(int id, int fotoId)
        {
            var foto = await _context.FotosVehiculo
                .FirstOrDefaultAsync(f => f.FotoId == fotoId && f.VehiculoId == id);

            if (foto == null) return NotFound();

            _context.FotosVehiculo.Remove(foto);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}