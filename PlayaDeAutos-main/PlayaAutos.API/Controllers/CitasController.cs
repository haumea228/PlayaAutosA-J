using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Models;
using PlayaAutos.API.Services;
using System.Security.Claims;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP,Vendedor")]
    public class CitasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificacionesService _notificaciones;

        // Ids de la tabla EstadosCita
        private const int EstadoConfirmada = 2;
        private const int EstadoCancelada = 4;

        public CitasController(AppDbContext context, NotificacionesService notificaciones)
        {
            _context = context;
            _notificaciones = notificaciones;
        }

        // GET: api/citas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CitaDto>>> GetCitas()
        {
            var citas = await _context.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(c => c.EstadoCita)
                .OrderByDescending(c => c.FechaHora)
                .Select(c => new CitaDto
                {
                    CitaId = c.CitaId,
                    ClienteId = c.ClienteId,
                    Cliente = c.Cliente.Nombre,
                    VehiculoId = c.VehiculoId,
                    Vehiculo = c.Vehiculo != null
                        ? c.Vehiculo.CodigoInterno + " - " + c.Vehiculo.Modelo.Marca.Nombre + " " + c.Vehiculo.Modelo.Nombre
                        : null,
                    VendedorId = c.VendedorId,
                    Vendedor = _context.Usuarios
                        .Where(u => u.UsuarioId == c.VendedorId)
                        .Select(u => u.UsuarioNombre)
                        .FirstOrDefault() ?? "Sin asignar",
                    EstadoCitaId = c.EstadoCitaId,
                    Estado = c.EstadoCita.Descripcion,
                    EstadoColor = c.EstadoCita.Color,
                    FechaHora = c.FechaHora,
                    TipoCita = c.TipoCita,
                    Observaciones = c.Observaciones
                })
                .ToListAsync();

            return Ok(citas);
        }

        // GET: api/citas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CitaDto>> GetCita(int id)
        {
            var c = await _context.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Vehiculo)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (c == null) return NotFound();

            var vendedor = await _context.Usuarios
                .Where(u => u.UsuarioId == c.VendedorId)
                .Select(u => u.UsuarioNombre)
                .FirstOrDefaultAsync();

            return new CitaDto
            {
                CitaId = c.CitaId,
                ClienteId = c.ClienteId,
                Cliente = c.Cliente.Nombre,
                VehiculoId = c.VehiculoId,
                Vehiculo = c.Vehiculo != null ? c.Vehiculo.CodigoInterno : null,
                VendedorId = c.VendedorId,
                Vendedor = vendedor ?? "Sin asignar",
                EstadoCitaId = c.EstadoCitaId,
                Estado = c.EstadoCita.Descripcion,
                EstadoColor = c.EstadoCita.Color,
                FechaHora = c.FechaHora,
                TipoCita = c.TipoCita,
                Observaciones = c.Observaciones
            };
        }

        // POST: api/citas
        [HttpPost]
        public async Task<ActionResult> PostCita(CrearCitaDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return BadRequest("El cliente no existe.");

            var vendedor = await _context.Usuarios.FindAsync(dto.VendedorId);
            if (vendedor == null)
                return BadRequest("El vendedor no existe.");

            var estadoCita = await _context.EstadosCita.FindAsync(
                dto.EstadoCitaId > 0 ? dto.EstadoCitaId : 1);
            if (estadoCita == null)
                return BadRequest("El estado de cita no existe.");
            // 🟢 Validar fecha/hora
            var errorFecha = ValidarFechaCita(dto.FechaHora);
            if (errorFecha != null)
                return BadRequest(errorFecha);
            var cita = new Cita
            {
                ClienteId = dto.ClienteId,
                VehiculoId = dto.VehiculoId,
                VendedorId = dto.VendedorId,
                EstadoCitaId = dto.EstadoCitaId > 0 ? dto.EstadoCitaId : 1,
                FechaHora = dto.FechaHora,
                TipoCita = dto.TipoCita,
                Observaciones = dto.Observaciones,
                FechaAlta = DateTime.Now
            };

            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCita), new { id = cita.CitaId },
                new { cita.CitaId });
        }

        // PUT: api/citas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCita(int id, CrearCitaDto dto)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();

            // 🟢 Validar fecha/hora
            var errorFecha = ValidarFechaCita(dto.FechaHora);
            if (errorFecha != null)
                return BadRequest(errorFecha);

            cita.ClienteId = dto.ClienteId;
            cita.VehiculoId = dto.VehiculoId;
            cita.VendedorId = dto.VendedorId;
            cita.FechaHora = dto.FechaHora;
            cita.TipoCita = dto.TipoCita;
            cita.Observaciones = dto.Observaciones;

            await _context.SaveChangesAsync();

            return Ok(new { cita.CitaId });
        }

        // PUT: api/citas/5/estado
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoCitaDto dto)
        {
            var cita = await _context.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Vehiculo).ThenInclude(v => v!.Modelo).ThenInclude(m => m.Marca)
                .FirstOrDefaultAsync(c => c.CitaId == id);
            if (cita == null) return NotFound();

            var estado = await _context.EstadosCita.FindAsync(dto.EstadoCitaId);
            if (estado == null)
                return BadRequest("El estado no existe.");

            var motivo = dto.Motivo?.Trim();
            if (motivo?.Length > 300) return BadRequest("El motivo no puede superar los 300 caracteres.");

            var estadoAnterior = cita.EstadoCitaId;
            cita.EstadoCitaId = dto.EstadoCitaId;

            // Motivo de cancelación: se agrega a la descripción, sin borrar lo
            // que el cliente haya escrito al agendar.
            if (dto.EstadoCitaId == EstadoCancelada && !string.IsNullOrEmpty(motivo))
            {
                var linea = $"Motivo de cancelación: {motivo}";
                cita.Observaciones = string.IsNullOrWhiteSpace(cita.Observaciones)
                    ? linea
                    : $"{cita.Observaciones.TrimEnd()}\n\n{linea}";
            }

            await _context.SaveChangesAsync();

            if (estadoAnterior != dto.EstadoCitaId &&
                (dto.EstadoCitaId == EstadoConfirmada || dto.EstadoCitaId == EstadoCancelada))
            {
                await NotificarCambioCitaAsync(cita, dto.EstadoCitaId == EstadoConfirmada, motivo);
            }

            return Ok(new { cita.CitaId, estado = estado.Descripcion });
        }

        // GET: api/citas/estados
        [HttpGet("estados")]
        public async Task<ActionResult<IEnumerable<EstadoCitaDto>>> GetEstadosCita()
        {
            var estados = await _context.EstadosCita
                .Select(e => new EstadoCitaDto
                {
                    EstadoCitaId = e.EstadoCitaId,
                    Descripcion = e.Descripcion,
                    Color = e.Color
                })
                .ToListAsync();

            return Ok(estados);
        }

        private Task NotificarCambioCitaAsync(Cita cita, bool confirmada, string? motivo = null)
        {
            var porque = string.IsNullOrEmpty(motivo) ? "" : $" Motivo: {motivo}.";
            var cuando = $"{cita.FechaHora:dd/MM} a las {cita.FechaHora:HH:mm}";
            var vehiculo = cita.Vehiculo != null
                ? $" ({cita.Vehiculo.Modelo.Marca.Nombre} {cita.Vehiculo.Modelo.Nombre})"
                : "";

            return confirmada
                ? _notificaciones.NotificarUsuarioAsync(cita.Cliente.UsuarioId,
                    "Cita confirmada",
                    $"Tu cita del {cuando}{vehiculo} fue confirmada. ¡Te esperamos!",
                    "cita", cita.CitaId)
                : _notificaciones.NotificarUsuarioAsync(cita.Cliente.UsuarioId,
                    "Cita cancelada",
                    $"Tu cita del {cuando}{vehiculo} fue cancelada.{porque} Podés agendar una nueva desde la app.",
                    "cita", cita.CitaId);
        }

        private string? ValidarFechaCita(DateTime fechaHora)
        {
            // No puede ser en el pasado
            if (fechaHora < DateTime.Now)
                return "La fecha y hora no puede ser en el pasado.";

            // Solo Lunes a Sábado (Domingo = 0)
            if (fechaHora.DayOfWeek == DayOfWeek.Sunday)
                return "No se agendan citas los domingos.";

            // Horario 08:00 a 18:00
            var hora = fechaHora.TimeOfDay;
            if (hora < TimeSpan.FromHours(8) || hora > TimeSpan.FromHours(18))
                return "Las citas deben agendarse entre las 08:00 y 18:00 hs.";

            // Feriados nacionales (fijos + Semana Santa)
            if (EsFeriado(fechaHora))
                return "No se agendan citas en días feriados.";

            return null; // Válido
        }

        private bool EsFeriado(DateTime fecha)
        {
            // Feriados fijos
            var feriados = new List<DateTime>
    {
        new(fecha.Year, 1, 1),   // Año Nuevo
        new(fecha.Year, 3, 1),   // Día de los Héroes
        new(fecha.Year, 5, 1),   // Día del Trabajador
        new(fecha.Year, 5, 14),  // Independencia
        new(fecha.Year, 5, 15),  // Independencia
        new(fecha.Year, 6, 12),  // Paz del Chaco
        new(fecha.Year, 8, 15),  // Fundación de Asunción
        new(fecha.Year, 9, 29),  // Boquerón
        new(fecha.Year, 12, 8),  // Virgen de Caacupé
        new(fecha.Year, 12, 25), // Navidad
    };

            // Semana Santa (cálculo)
            var ss = CalcularSemanaSanta(fecha.Year);
            feriados.Add(ss.JuevesSanto);
            feriados.Add(ss.ViernesSanto);

            return feriados.Any(f => f.Date == fecha.Date);
        }

        private (DateTime JuevesSanto, DateTime ViernesSanto) CalcularSemanaSanta(int año)
        {
            int a = año % 19, b = año % 4, c = año % 7;
            int d = (19 * a + 24) % 30;
            int e = (2 * b + 4 * c + 6 * d + 5) % 7;
            int dias = 22 + d + e;
            DateTime pascua = dias <= 31 ? new(año, 3, dias) : new(año, 4, dias - 31);
            return (pascua.AddDays(-3), pascua.AddDays(-2));
        }
    }
}