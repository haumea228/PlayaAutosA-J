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
    public class ConsignasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ConsignasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/consignas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsignaListDto>>> GetConsignas()
        {
            await ExpirarVencidas();

            var list = await _context.ContratosConsigna
                .Include(c => c.Consignante).ThenInclude(con => con.Cliente)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .OrderByDescending(c => c.FechaGeneracion)
                .ToListAsync();

            return list.Select(MapListDto).ToList();
        }

        // GET: api/consignas/{id}/contrato
        [HttpGet("{id}/contrato")]
        public async Task<ActionResult<ConsignaContratoDto>> GetContrato(int id)
        {
            var c = await _context.ContratosConsigna
                .Include(c => c.Consignante).ThenInclude(con => con.Cliente)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Tipo)
                .Include(c => c.Tasacion)
                .FirstOrDefaultAsync(c => c.ContratoId == id);

            if (c == null) return NotFound();

            var precioCliente = c.Tasacion?.ValorTasacion ?? c.Vehiculo.CostoAdquisicion ?? 0;

            return new ConsignaContratoDto
            {
                ContratoId = c.ContratoId,
                NumeroContrato = c.NumeroContrato,
                FechaInicio = c.FechaInicio,
                FechaFin = c.FechaFin,
                DuracionDias = (int)(c.FechaFin - c.FechaInicio).TotalDays,
                PorcentajeComision = c.PorcentajeComision,
                Estado = c.Estado,
                Clausulas = c.Clausulas,
                FechaGeneracion = c.FechaGeneracion,
                ConsignanteNombre = c.Consignante.Cliente.Nombre,
                ConsignanteCI = c.Consignante.Cliente.CI_RUC,
                VehiculoMarca = c.Vehiculo.Modelo.Marca.Nombre,
                VehiculoModelo = c.Vehiculo.Modelo.Nombre,
                VehiculoAnio = c.Vehiculo.Anio,
                VehiculoColor = c.Vehiculo.Color,
                VehiculoKilometraje = c.Vehiculo.Kilometraje,
                VehiculoTipo = c.Vehiculo.Tipo?.Descripcion,
                PrecioVentaCliente = precioCliente,
                PrecioVentaPlaya = c.Vehiculo.PrecioVenta
            };
        }

        // GET: api/consignas/consignantes
        [HttpGet("consignantes")]
        public async Task<ActionResult<IEnumerable<ConsignanteListDto>>> GetConsignantes()
        {
            var list = await _context.Consignantes
                .Include(c => c.Cliente)
                .Where(c => c.Activo)
                .ToListAsync();

            return list.Select(c => new ConsignanteListDto
            {
                ConsignanteId = c.ConsignanteId,
                ClienteId = c.ClienteId,
                Nombre = c.Cliente.Nombre,
                CI_RUC = c.Cliente.CI_RUC,
                PorcentajeComisionDefault = c.PorcentajeComisionDefault
            }).ToList();
        }

        // GET: api/consignas/tasaciones/{consignanteId}
        [HttpGet("tasaciones/{consignanteId}")]
        public async Task<ActionResult<IEnumerable<TasacionParaConsignaDto>>> GetTasacionesParaConsigna(int consignanteId)
        {
            var consignante = await _context.Consignantes
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.ConsignanteId == consignanteId);

            if (consignante == null) return NotFound();

            var tasaciones = await _context.TasacionesVehiculo
                .Where(t =>
                    t.ClienteId == consignante.ClienteId &&
                    t.EstadoTasacion == "Aprobada")
                .ToListAsync();

            var result = new List<TasacionParaConsignaDto>();
            foreach (var t in tasaciones)
            {
                if (t.VehiculoId.HasValue)
                {
                    // Si ya tiene vehículo con consigna vigente, no la mostramos
                    var tieneActiva = await _context.ContratosConsigna
                        .AnyAsync(c => c.VehiculoId == t.VehiculoId && c.Estado == "Vigente");
                    if (tieneActiva) continue;
                }

                bool tieneDatosCatalogo = t.ModeloId.HasValue && t.TipoId.HasValue
                                          && t.CondicionId.HasValue && t.OrigenId.HasValue;

                var descripcion = $"{t.MarcaVehiculo} {t.ModeloVehiculo} ({t.AnhoVehiculo})";
                if (!tieneDatosCatalogo && !t.VehiculoId.HasValue)
                    descripcion += " ⚠ faltan datos del catálogo";

                result.Add(new TasacionParaConsignaDto
                {
                    TasacionVehiculoId = t.TasacionVehiculoId,
                    VehiculoId = t.VehiculoId,
                    Descripcion = descripcion,
                    PrecioVentaCliente = t.PrecioVenta,
                    ModeloId = t.ModeloId,
                    TipoId = t.TipoId,
                    CondicionId = t.CondicionId,
                    OrigenId = t.OrigenId,
                    Color = t.ColorVehiculo,
                    Kilometraje = t.KilometrajeVehiculo,
                    Anio = t.AnhoVehiculo
                });
            }

            return result;
        }

        // POST: api/consignas
        [HttpPost]
        public async Task<IActionResult> CrearConsigna(CrearConsignaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.FechaFin <= dto.FechaInicio) return BadRequest("La fecha de fin debe ser posterior a la fecha de inicio.");
            if (dto.PorcentajeComision <= 0 || dto.PorcentajeComision > 100) return BadRequest("El porcentaje debe estar entre 0 y 100.");

            var consignante = await _context.Consignantes
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.ConsignanteId == dto.ConsignanteId);
            if (consignante == null) return BadRequest("Consignante no encontrado.");

            var tasacion = await _context.TasacionesVehiculo
                .FirstOrDefaultAsync(t => t.TasacionVehiculoId == dto.TasacionVehiculoId);
            if (tasacion == null) return BadRequest("Tasación no encontrada.");
            if (tasacion.EstadoTasacion != "Aprobada") return BadRequest("Solo se pueden consignar tasaciones aprobadas.");
            if (tasacion.ClienteId != consignante.ClienteId) return BadRequest("La tasación no pertenece a este consignante.");

            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int vehiculoId;

                if (tasacion.VehiculoId.HasValue)
                {
                    vehiculoId = tasacion.VehiculoId.Value;
                    var tieneActiva = await _context.ContratosConsigna
                        .AnyAsync(c => c.VehiculoId == vehiculoId && c.Estado == "Vigente");
                    if (tieneActiva) return BadRequest("Este vehículo ya tiene una consigna vigente.");
                }
                else
                {
                    if (!tasacion.ModeloId.HasValue || !tasacion.TipoId.HasValue ||
                        !tasacion.CondicionId.HasValue || !tasacion.OrigenId.HasValue)
                        return BadRequest("La tasación no tiene todos los datos del catálogo. Apruébela indicando modelo, tipo, condición y origen.");

                    var estadoDisponible = await _context.EstadosVehiculo
                        .FirstOrDefaultAsync(e => e.VisibleEnCatalogo && e.PermiteVenta);
                    if (estadoDisponible == null)
                        return BadRequest("No existe un estado de vehículo disponible para el catálogo.");

                    var codigoInterno = $"CS-{DateTime.Now:yyyyMMddHHmmss}";

                    var vehiculoNuevo = new Vehiculo
                    {
                        CodigoInterno = codigoInterno,
                        ModeloId = tasacion.ModeloId.Value,
                        TipoId = tasacion.TipoId.Value,
                        CondicionId = tasacion.CondicionId.Value,
                        EstadoId = estadoDisponible.EstadoId,
                        OrigenId = tasacion.OrigenId.Value,
                        Anio = tasacion.AnhoVehiculo,
                        Color = tasacion.ColorVehiculo,
                        Kilometraje = tasacion.KilometrajeVehiculo,
                        PrecioVenta = tasacion.PrecioVenta,
                        CostoAdquisicion = tasacion.ValorTasacion,
                        ConsignanteId = dto.ConsignanteId,
                        PorcentajeComision = dto.PorcentajeComision,
                        FechaConsigna = dto.FechaInicio,
                        FechaAlta = DateTime.Now,
                        UsuarioAlta = usuarioId
                    };

                    _context.Vehiculos.Add(vehiculoNuevo);
                    await _context.SaveChangesAsync();

                    tasacion.VehiculoId = vehiculoNuevo.VehiculoId;
                    vehiculoId = vehiculoNuevo.VehiculoId;
                }

                var vehiculo = await _context.Vehiculos.FindAsync(vehiculoId);
                if (vehiculo == null) return BadRequest("Vehículo no encontrado.");

                var estadoCatalogo = await _context.EstadosVehiculo
                    .FirstOrDefaultAsync(e => e.VisibleEnCatalogo && e.PermiteVenta);
                if (estadoCatalogo != null)
                    vehiculo.EstadoId = estadoCatalogo.EstadoId;

                vehiculo.ConsignanteId = dto.ConsignanteId;
                vehiculo.PorcentajeComision = dto.PorcentajeComision;
                vehiculo.FechaConsigna = dto.FechaInicio;

                var count = await _context.ContratosConsigna.CountAsync() + 1;
                var numeroContrato = $"CONS-{DateTime.Now.Year}-{count:D4}";

                var contrato = new ContratoConsigna
                {
                    VehiculoId = vehiculoId,
                    ConsignanteId = dto.ConsignanteId,
                    TasacionVehiculoId = dto.TasacionVehiculoId,
                    NumeroContrato = numeroContrato,
                    FechaInicio = dto.FechaInicio,
                    FechaFin = dto.FechaFin,
                    PorcentajeComision = dto.PorcentajeComision,
                    Clausulas = dto.Clausulas,
                    Estado = "Vigente",
                    FechaGeneracion = DateTime.Now
                };

                _context.ContratosConsigna.Add(contrato);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { contratoId = contrato.ContratoId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al crear la consigna: {ex.Message}");
            }
        }

        // PUT: api/consignas/{id}/anular
        [HttpPut("{id}/anular")]
        public async Task<IActionResult> AnularConsigna(int id)
        {
            var contrato = await _context.ContratosConsigna
                .Include(c => c.Vehiculo)
                .FirstOrDefaultAsync(c => c.ContratoId == id);

            if (contrato == null) return NotFound();
            if (contrato.Estado != "Vigente") return BadRequest("Solo se pueden anular contratos vigentes.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                contrato.Estado = "Anulado";

                var estadoInactivo = await _context.EstadosVehiculo
                    .FirstOrDefaultAsync(e => !e.VisibleEnCatalogo);

                if (contrato.Vehiculo != null)
                {
                    if (estadoInactivo != null)
                        contrato.Vehiculo.EstadoId = estadoInactivo.EstadoId;
                    contrato.Vehiculo.ConsignanteId = null;
                    contrato.Vehiculo.PorcentajeComision = null;
                    contrato.Vehiculo.FechaConsigna = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error al anular la consigna.");
            }
        }

        private async Task ExpirarVencidas()
        {
            var hoy = DateTime.Today;
            var vencidos = await _context.ContratosConsigna
                .Include(c => c.Vehiculo)
                .Where(c => c.Estado == "Vigente" && c.FechaFin < hoy)
                .ToListAsync();

            if (!vencidos.Any()) return;

            var estadoInactivo = await _context.EstadosVehiculo
                .FirstOrDefaultAsync(e => !e.VisibleEnCatalogo);

            foreach (var c in vencidos)
            {
                c.Estado = "Vencido";
                if (estadoInactivo != null && c.Vehiculo != null)
                {
                    c.Vehiculo.EstadoId = estadoInactivo.EstadoId;
                    c.Vehiculo.ConsignanteId = null;
                    c.Vehiculo.PorcentajeComision = null;
                    c.Vehiculo.FechaConsigna = null;
                }
            }

            await _context.SaveChangesAsync();
        }

        private static ConsignaListDto MapListDto(ContratoConsigna c) => new()
        {
            ContratoId = c.ContratoId,
            NumeroContrato = c.NumeroContrato,
            ConsignanteId = c.ConsignanteId,
            Consignante = c.Consignante.Cliente.Nombre,
            ConsignanteCI = c.Consignante.Cliente.CI_RUC ?? "",
            VehiculoId = c.VehiculoId,
            Vehiculo = $"{c.Vehiculo.Modelo.Marca.Nombre} {c.Vehiculo.Modelo.Nombre} {c.Vehiculo.Anio}",
            FechaInicio = c.FechaInicio,
            FechaFin = c.FechaFin,
            PorcentajeComision = c.PorcentajeComision,
            PrecioCliente = c.Vehiculo.PrecioVenta,
            Estado = c.Estado,
            FechaGeneracion = c.FechaGeneracion
        };
    }
}
