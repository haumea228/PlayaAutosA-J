using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Helpers;
using PlayaAutos.API.Models;
using PlayaAutos.API.Services;
using System.Security.Claims;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/micuenta")]
    [Authorize(Roles = "Cliente")]
    public class MiCuentaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ComprobanteStorage _comprobantes;
        private readonly CuotaPagoService _pagoService;
        private readonly PagoOnlineService _pagosOnline;

        public MiCuentaController(AppDbContext context, ComprobanteStorage comprobantes,
            CuotaPagoService pagoService, PagoOnlineService pagosOnline)
        {
            _context = context;
            _comprobantes = comprobantes;
            _pagoService = pagoService;
            _pagosOnline = pagosOnline;
        }

        private const string MensajeCiDuplicado =
            "Ese CI/RUC ya está registrado en otra cuenta. Si es tuyo, comunicate con la playa.";

        /// <summary>Formato y unicidad de un CI/RUC que el cliente carga por primera vez.</summary>
        private async Task<string?> ValidarCiNuevoAsync(string ciNormalizado, int clienteId)
        {
            if (!CiRuc.EsValido(ciNormalizado)) return CiRuc.MensajeFormato;
            var enUso = await _context.Clientes.AnyAsync(c => c.CI_RUC == ciNormalizado && c.ClienteId != clienteId);
            return enUso ? MensajeCiDuplicado : null;
        }

        private int? GetUsuarioId()
        {
            if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id))
                return id;
            return null;
        }

        // GET: api/micuenta/perfil
        [HttpGet("perfil")]
        public async Task<ActionResult<ClienteDto>> GetPerfil()
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var c = await _context.Clientes.FirstOrDefaultAsync(x => x.UsuarioId == uid);
            if (c == null) return NotFound("No se encontró perfil de cliente.");

            return new ClienteDto
            {
                ClienteId = c.ClienteId,
                Nombre    = c.Nombre,
                CI_RUC    = c.CI_RUC,
                Telefono  = c.Telefono,
                Email     = c.Email,
                Direccion = c.Direccion,
                Activo    = c.Activo
            };
        }

        // PUT: api/micuenta/perfil
        [HttpPut("perfil")]
        public async Task<IActionResult> PutPerfil(ActualizarPerfilClienteDto dto)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(x => x.UsuarioId == uid);
            if (cliente == null) return NotFound("No se encontró perfil de cliente.");

            // CI/RUC: se carga una única vez. Si ya está registrado no se puede
            // cambiar desde la app (lo corrige un administrador desde la web).
            var ciNuevo = CiRuc.Normalizar(dto.CI_RUC);
            if (ciNuevo != null)
            {
                if (!string.IsNullOrEmpty(cliente.CI_RUC))
                {
                    if (ciNuevo != cliente.CI_RUC)
                        return BadRequest("Tu CI/RUC ya está registrado y no se puede modificar desde la app. Si es incorrecto, solicitá la corrección a la playa.");
                }
                else
                {
                    var error = await ValidarCiNuevoAsync(ciNuevo, cliente.ClienteId);
                    if (error != null) return BadRequest(error);
                    cliente.CI_RUC = ciNuevo;
                }
            }

            cliente.Nombre    = dto.Nombre;
            cliente.Telefono  = dto.Telefono;
            cliente.Direccion = dto.Direccion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Índice único de CI_RUC: otro cliente lo registró al mismo tiempo.
                return BadRequest(MensajeCiDuplicado);
            }
            return Ok(new { cliente.ClienteId, cliente.CI_RUC });
        }

        // POST: api/micuenta/dispositivos — registra (o reasigna) el token FCM
        // del dispositivo para recibir notificaciones push.
        [HttpPost("dispositivos")]
        public async Task<IActionResult> RegistrarDispositivo(DispositivoDto dto)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var token = dto.Token?.Trim() ?? "";
            if (token.Length == 0 || token.Length > 512) return BadRequest("Token de dispositivo inválido.");

            var ahora = DateTime.Now;
            var existente = await _context.DispositivosNotificacion.FirstOrDefaultAsync(d => d.Token == token);
            if (existente == null)
            {
                _context.DispositivosNotificacion.Add(new DispositivoNotificacion
                {
                    UsuarioId = uid.Value,
                    Token = token,
                    Plataforma = dto.Plataforma,
                    FechaRegistro = ahora,
                    FechaUltimoUso = ahora
                });
            }
            else
            {
                // Mismo teléfono con otra cuenta: el token pasa al usuario actual
                // para que el anterior deje de recibir avisos en este equipo.
                existente.UsuarioId = uid.Value;
                existente.Plataforma = dto.Plataforma ?? existente.Plataforma;
                existente.FechaUltimoUso = ahora;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Registro simultáneo del mismo token: ya quedó guardado por el otro request.
            }
            return NoContent();
        }

        // DELETE: api/micuenta/dispositivos — se llama al cerrar sesión.
        [HttpDelete("dispositivos")]
        public async Task<IActionResult> EliminarDispositivo(DispositivoDto dto)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            await _context.DispositivosNotificacion
                .Where(d => d.Token == dto.Token && d.UsuarioId == uid)
                .ExecuteDeleteAsync();
            return NoContent();
        }

        // GET: api/micuenta/citas
        [HttpGet("citas")]
        public async Task<ActionResult<IEnumerable<CitaDto>>> GetMisCitas()
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return Ok(new List<CitaDto>());

            var citas = await _context.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(c => c.EstadoCita)
                .Where(c => c.ClienteId == cliente.ClienteId)
                .OrderByDescending(c => c.FechaHora)
                .Select(c => new CitaDto
                {
                    CitaId       = c.CitaId,
                    ClienteId    = c.ClienteId,
                    Cliente      = c.Cliente.Nombre,
                    VehiculoId   = c.VehiculoId,
                    Vehiculo     = c.Vehiculo != null
                        ? c.Vehiculo.Modelo.Marca.Nombre + " " + c.Vehiculo.Modelo.Nombre
                        : null,
                    VendedorId   = c.VendedorId,
                    Vendedor     = _context.Usuarios
                        .Where(u => u.UsuarioId == c.VendedorId)
                        .Select(u => u.UsuarioNombre)
                        .FirstOrDefault() ?? "Sin asignar",
                    EstadoCitaId = c.EstadoCitaId,
                    Estado       = c.EstadoCita.Descripcion,
                    EstadoColor  = c.EstadoCita.Color,
                    FechaHora    = c.FechaHora,
                    TipoCita     = c.TipoCita,
                    Observaciones = c.Observaciones
                })
                .ToListAsync();

            return Ok(citas);
        }

        // POST: api/micuenta/citas/agendar
        [HttpPost("citas/agendar")]
        public async Task<ActionResult> AgendarCita(AgendarCitaClienteDto dto)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return BadRequest("No se encontró perfil de cliente.");

            // Solo se actualizan los datos que vengan (portal web); la app no los
            // envía y se usan los que ya tiene el perfil.
            if (!string.IsNullOrWhiteSpace(dto.Nombre))    cliente.Nombre    = dto.Nombre.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Telefono))  cliente.Telefono  = dto.Telefono.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Direccion)) cliente.Direccion = dto.Direccion.Trim();

            // Para poder contactarlo, el perfil tiene que tener nombre y teléfono.
            if (string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Telefono))
                return BadRequest("Completá tu nombre y teléfono en tu perfil antes de agendar una cita.");

            var ciCita = CiRuc.Normalizar(dto.CI_RUC);
            if (string.IsNullOrEmpty(cliente.CI_RUC) && ciCita != null)
            {
                var errorCi = await ValidarCiNuevoAsync(ciCita, cliente.ClienteId);
                if (errorCi != null) return BadRequest(errorCi);
                cliente.CI_RUC = ciCita;
            }

            var vendedorId = await _context.Usuarios
                .Where(u => u.Rol == "Vendedor" || u.Rol == "AdministradorP")
                .Select(u => (int?)u.UsuarioId)
                .FirstOrDefaultAsync();

            if (vendedorId == null)
                return BadRequest("No hay vendedores disponibles en el sistema.");

            var cita = new Cita
            {
                ClienteId     = cliente.ClienteId,
                VehiculoId    = dto.VehiculoId,
                VendedorId    = vendedorId.Value,
                EstadoCitaId  = 1,
                FechaHora     = dto.FechaHora,
                TipoCita      = dto.TipoCita,
                Observaciones = dto.Observaciones,
                FechaAlta     = DateTime.Now
            };

            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();

            return Ok(new { cita.CitaId });
        }

        // PUT: api/micuenta/citas/{id}/cancelar
        [HttpPut("citas/{id}/cancelar")]
        public async Task<IActionResult> CancelarCita(int id,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)]
            CancelarCitaClienteDto? dto)
        {
            var motivo = dto?.Motivo?.Trim();
            if (motivo?.Length > 300) return BadRequest("El motivo no puede superar los 300 caracteres.");

            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return Unauthorized();

            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();
            if (cita.ClienteId != cliente.ClienteId) return Forbid();

            if (cita.EstadoCitaId == 3 || cita.EstadoCitaId == 4)
                return BadRequest("No se puede cancelar una cita ya realizada o cancelada.");
            if (cita.FechaHora < DateTime.Now)
                return BadRequest("No se puede cancelar una cita cuyo horario ya pasó.");

            cita.EstadoCitaId = 4;

            // Queda registrado en la descripción (visible en la web y en la app)
            // que la canceló el propio cliente, sin borrar lo que ya había.
            var linea = string.IsNullOrEmpty(motivo)
                ? "Cancelada por el cliente."
                : $"Cancelada por el cliente. Motivo: {motivo}";
            cita.Observaciones = string.IsNullOrWhiteSpace(cita.Observaciones)
                ? linea
                : $"{cita.Observaciones.TrimEnd()}\n\n{linea}";

            await _context.SaveChangesAsync();

            return Ok(new { cita.CitaId });
        }

        // GET: api/micuenta/facturas
        [HttpGet("facturas")]
        public async Task<ActionResult<IEnumerable<FacturaDto>>> GetMisFacturas()
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return Ok(new List<FacturaDto>());

            var facturas = await _context.Facturas
                .Include(f => f.Venta).ThenInclude(v => v.Cliente)
                .Include(f => f.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(f => f.Timbrado)
                .Where(f => f.Venta.ClienteId == cliente.ClienteId)
                .OrderByDescending(f => f.FechaEmision)
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

            return Ok(facturas);
        }

        // GET: api/micuenta/facturas/{id}/pdf
        [HttpGet("facturas/{id}/pdf")]
        public async Task<IActionResult> GetMiFacturaPdf(int id)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return NotFound();

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
                .FirstOrDefaultAsync(f => f.FacturaId == id && f.Venta.ClienteId == cliente.ClienteId);

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

        // GET: api/micuenta/notascredito
        [HttpGet("notascredito")]
        public async Task<ActionResult<IEnumerable<NotaCreditoDto>>> GetMisNotasCredito()
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return Ok(new List<NotaCreditoDto>());

            var notas = await _context.NotasCredito
                .Include(n => n.Venta).ThenInclude(v => v.Cliente)
                .Include(n => n.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(n => n.Venta).ThenInclude(v => v.Factura)
                .Include(n => n.Timbrado)
                .Include(n => n.TasacionVehiculo)
                .Where(n => n.Venta.ClienteId == cliente.ClienteId)
                .OrderByDescending(n => n.FechaEmision)
                .ToListAsync();

            return Ok(notas.Select(MapNotaCreditoDto).ToList());
        }

        // GET: api/micuenta/notascredito/{id}/pdf
        [HttpGet("notascredito/{id}/pdf")]
        public async Task<IActionResult> GetMiNotaCreditoPdf(int id)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return NotFound();

            var n = await _context.NotasCredito
                .Include(n => n.Timbrado)
                .Include(n => n.Venta).ThenInclude(v => v.Cliente)
                .Include(n => n.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(n => n.Venta).ThenInclude(v => v.Factura)
                .Include(n => n.TasacionVehiculo)
                .FirstOrDefaultAsync(n => n.NotaCreditoId == id && n.Venta.ClienteId == cliente.ClienteId);

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

        // GET: api/micuenta/contratos
        [HttpGet("contratos")]
        public async Task<ActionResult<IEnumerable<ConsignaContratoDto>>> GetMisContratos()
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return Ok(new List<ConsignaContratoDto>());

            var consignante = await _context.Consignantes
                .FirstOrDefaultAsync(co => co.ClienteId == cliente.ClienteId);
            if (consignante == null) return Ok(new List<ConsignaContratoDto>());

            var contratos = await _context.ContratosConsigna
                .Include(c => c.Consignante).ThenInclude(con => con.Cliente)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Modelo).ThenInclude(m => m.Marca)
                .Include(c => c.Vehiculo).ThenInclude(v => v.Tipo)
                .Include(c => c.Tasacion)
                .Where(c => c.ConsignanteId == consignante.ConsignanteId)
                .OrderByDescending(c => c.FechaGeneracion)
                .ToListAsync();

            return Ok(contratos.Select(MapContratoDto).ToList());
        }

        // GET: api/micuenta/cuotas
        [HttpGet("cuotas")]
        public async Task<ActionResult<IEnumerable<MiCuotaDto>>> GetMisCuotas(CancellationToken ct)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return Ok(new List<MiCuotaDto>());

            // Pagos con tarjeta que quedaron sin confirmar (p. ej. pagó y cerró la
            // app antes de volver): se verifican contra Stripe antes de listar.
            await _pagosOnline.ConciliarPendientesAsync(cliente.ClienteId, ct);

            // Se proyecta solo lo necesario: ComprobanteImagen (base64 de los cobros
            // en ventanilla) no se envía a la app.
            var filas = await _context.Cuotas
                .AsNoTracking()
                .Where(c => c.Venta.ClienteId == cliente.ClienteId && c.Venta.Estado != "Anulada")
                .Select(c => new
                {
                    Cuota = new Cuota
                    {
                        CuotaId = c.CuotaId,
                        VentaId = c.VentaId,
                        NumeroCuota = c.NumeroCuota,
                        Monto = c.Monto,
                        FechaVencimiento = c.FechaVencimiento,
                        FechaPago = c.FechaPago,
                        MontoPagado = c.MontoPagado,
                        MontoRecargo = c.MontoRecargo,
                        Estado = c.Estado
                    },
                    c.Venta.PorcentajeRecargo,
                    c.Venta.CantidadCuotas,
                    Cliente = c.Venta.Cliente.Nombre,
                    Vehiculo = c.Venta.Vehiculo.Modelo.Marca.Nombre + " " + c.Venta.Vehiculo.Modelo.Nombre,
                    UltimaSolicitud = c.SolicitudesPago
                        .OrderByDescending(s => s.SolicitudPagoCuotaId)
                        .Select(s => new { s.Estado, s.FechaReporte, s.MotivoRechazo })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var ahora = DateTime.Now;

            // La cuota que se puede pagar es la de menor número entre las no
            // pagadas de cada venta (las pagadas antes mantienen el saldo del
            // ticket en orden).
            var primeraNoPagadaPorVenta = filas
                .Where(f => f.Cuota.Estado == EstadosCuota.Pendiente || f.Cuota.Estado == EstadosCuota.EnRevision)
                .GroupBy(f => f.Cuota.VentaId)
                .ToDictionary(g => g.Key, g => g.Min(f => f.Cuota.NumeroCuota));

            var resultado = filas
                .OrderBy(f => f.Cuota.FechaVencimiento)
                .ThenBy(f => f.Cuota.NumeroCuota)
                .Select(f =>
                {
                    var c = f.Cuota;
                    var recargo = c.Estado == EstadosCuota.Pendiente
                        ? CuotaPagoService.CalcularRecargo(c, new Venta { PorcentajeRecargo = f.PorcentajeRecargo }, ahora)
                        : 0;

                    string? motivo = null;
                    if (c.Estado == EstadosCuota.EnRevision)
                        motivo = "Tu pago está en revisión.";
                    else if (c.Estado == EstadosCuota.Pendiente &&
                             primeraNoPagadaPorVenta.TryGetValue(c.VentaId, out var primera) &&
                             primera != c.NumeroCuota)
                        motivo = $"Primero tenés que abonar la cuota N° {primera}.";

                    return new MiCuotaDto
                    {
                        CuotaId = c.CuotaId,
                        VentaId = c.VentaId,
                        NumeroCuota = c.NumeroCuota,
                        CantidadCuotas = f.CantidadCuotas,
                        Monto = c.Monto,
                        FechaVencimiento = c.FechaVencimiento,
                        FechaPago = c.FechaPago,
                        MontoPagado = c.MontoPagado,
                        MontoRecargo = c.MontoRecargo,
                        Estado = c.Estado,
                        Cliente = f.Cliente,
                        Vehiculo = f.Vehiculo,
                        RecargoCalculado = recargo,
                        MontoAPagar = c.Monto + recargo,
                        PuedeInformarPago = c.Estado == EstadosCuota.Pendiente && motivo == null,
                        PagoConTarjetaDisponible = _pagosOnline.Habilitado,
                        MotivoNoPuedePagar = motivo,
                        UltimaSolicitudEstado = f.UltimaSolicitud?.Estado,
                        UltimaSolicitudFecha = f.UltimaSolicitud?.FechaReporte,
                        UltimaSolicitudMotivoRechazo = f.UltimaSolicitud?.MotivoRechazo
                    };
                })
                .ToList();

            return Ok(resultado);
        }

        // POST: api/micuenta/cuotas/{id}/reportar-pago  (multipart/form-data)
        // El cliente informa que pagó por transferencia y adjunta la foto del
        // comprobante. La cuota queda "EnRevision" hasta que un Cajero o
        // Administrador apruebe o rechace el pago desde la web (Pendientes).
        [HttpPost("cuotas/{id}/reportar-pago")]
        [RequestSizeLimit(ComprobanteStorage.TamanoMaximoBytes + 1024 * 1024)]
        public async Task<IActionResult> ReportarPagoCuota(int id, IFormFile? comprobante, [FromForm] string? observacion,
            CancellationToken ct)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return BadRequest("No se encontró perfil de cliente.");

            if (!await _context.Cuotas.AnyAsync(c => c.CuotaId == id && c.Venta.ClienteId == cliente.ClienteId, ct))
                return NotFound("Cuota no encontrada.");

            // Una página de pago con tarjeta abierta para esta cuota se verifica
            // (por si ya se pagó) y se cierra, para que no se cobre dos veces.
            await _pagosOnline.CerrarPendientesAsync(id, ct);

            var cuota = await _context.Cuotas.Include(c => c.Venta).FirstAsync(c => c.CuotaId == id, ct);
            var errorPago = await _pagoService.ValidarPagoClienteAsync(cuota);
            if (errorPago != null) return BadRequest(errorPago);

            var obs = observacion?.Trim();
            if (obs?.Length > 500) return BadRequest("La observación no puede superar los 500 caracteres.");

            string archivo;
            string contentType;
            try
            {
                (archivo, contentType) = await _comprobantes.GuardarAsync(comprobante!);
            }
            catch (ComprobanteInvalidoException ex)
            {
                return BadRequest(ex.Message);
            }

            var ahora = DateTime.Now;
            var solicitud = new SolicitudPagoCuota
            {
                CuotaId = cuota.CuotaId,
                ClienteId = cliente.ClienteId,
                FechaReporte = ahora,
                Monto = cuota.Monto,
                MontoRecargo = CuotaPagoService.CalcularRecargo(cuota, cuota.Venta, ahora),
                ComprobanteArchivo = archivo,
                ComprobanteContentType = contentType,
                ObservacionCliente = string.IsNullOrEmpty(obs) ? null : obs,
                Estado = EstadosSolicitudPago.Pendiente
            };

            _context.SolicitudesPagoCuota.Add(solicitud);
            cuota.Estado = EstadosCuota.EnRevision;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Típicamente el índice único (una sola solicitud pendiente por
                // cuota) ante un doble envío simultáneo.
                _comprobantes.Eliminar(archivo);
                return Conflict("Ya hay un pago informado para esta cuota.");
            }

            return Ok(new
            {
                solicitud.SolicitudPagoCuotaId,
                solicitud.CuotaId,
                Total = solicitud.Monto + solicitud.MontoRecargo,
                mensaje = "Recibimos tu comprobante. Te avisaremos cuando se confirme el pago."
            });
        }

        // POST: api/micuenta/cuotas/{id}/pago-online
        // Crea la página de pago con tarjeta (Stripe Checkout) de la cuota.
        [HttpPost("cuotas/{id}/pago-online")]
        public async Task<ActionResult<PagoOnlineDto>> IniciarPagoOnline(int id, CancellationToken ct)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid, ct);
            if (cliente == null) return BadRequest("No se encontró perfil de cliente.");

            var (pago, error) = await _pagosOnline.IniciarAsync(cliente, id, ct);
            if (pago == null) return BadRequest(error);

            return MapPagoOnline(pago);
        }

        // POST: api/micuenta/pagos-online/{id}/verificar
        // La app lo llama al volver de la página de pago: consulta a Stripe y,
        // si se cobró, registra el pago de la cuota.
        [HttpPost("pagos-online/{id}/verificar")]
        public async Task<ActionResult<PagoOnlineDto>> VerificarPagoOnline(int id, CancellationToken ct)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var esDelCliente = await _context.PagosOnlineCuota
                .AnyAsync(p => p.PagoOnlineCuotaId == id && p.Cliente.UsuarioId == uid, ct);
            if (!esDelCliente) return NotFound();

            try
            {
                return MapPagoOnline(await _pagosOnline.ConfirmarAsync(id, ct));
            }
            catch (Stripe.StripeException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    "No se pudo consultar el estado del pago. Probá de nuevo en un momento.");
            }
        }

        private static PagoOnlineDto MapPagoOnline(PagoOnlineCuota p) => new()
        {
            PagoOnlineId = p.PagoOnlineCuotaId,
            CuotaId = p.CuotaId,
            Url = p.Estado == EstadosPagoOnline.Pendiente ? p.UrlPago : null,
            Total = p.Monto + p.MontoRecargo,
            Estado = p.Estado,
            FechaExpiracion = p.FechaExpiracion
        };

        // GET: api/micuenta/cuotas/{id}/ticket
        [HttpGet("cuotas/{id}/ticket")]
        public async Task<IActionResult> GetMiTicketCuota(int id)
        {
            var uid = GetUsuarioId();
            if (uid == null) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == uid);
            if (cliente == null) return NotFound();

            var cuota = await _context.Cuotas
                .Include(c => c.Venta).ThenInclude(v => v.Cliente)
                .Include(c => c.Venta).ThenInclude(v => v.Vehiculo)
                    .ThenInclude(ve => ve.Modelo).ThenInclude(m => m.Marca)
                .Include(c => c.Venta).ThenInclude(v => v.Cuotas)
                .Include(c => c.Venta).ThenInclude(v => v.Vendedor)
                .Include(c => c.FormaPago)
                .FirstOrDefaultAsync(c => c.CuotaId == id && c.Venta.ClienteId == cliente.ClienteId);

            if (cuota == null) return NotFound();
            if (cuota.Estado != "Pagada") return BadRequest("La cuota aún no ha sido pagada.");

            var venta = cuota.Venta;
            var saldoInicial = venta.SaldoFinanciado ?? venta.MontoTotal;

            var pagosAnteriores = venta.Cuotas
                .Where(c => c.Estado == "Pagada" && c.NumeroCuota < cuota.NumeroCuota)
                .Sum(c => (c.MontoPagado ?? 0) + (c.MontoRecargo ?? 0));

            var saldoAnterior = saldoInicial - pagosAnteriores;
            var abono = cuota.MontoPagado ?? cuota.Monto;
            var recargo = cuota.MontoRecargo ?? 0;
            var saldoActual = saldoAnterior - abono;

            var data = new TicketCuotaData
            {
                CuotaId = cuota.CuotaId,
                NumeroCuota = cuota.NumeroCuota,
                CantidadCuotas = venta.CantidadCuotas ?? venta.Cuotas.Count,
                Cliente = venta.Cliente.Nombre,
                Vehiculo = $"{venta.Vehiculo.Modelo.Marca.Nombre} {venta.Vehiculo.Modelo.Nombre}",
                FormaPago = cuota.FormaPago?.Descripcion ?? "—",
                Observacion = cuota.ObservacionPago,
                FechaPago = cuota.FechaRegistroPago ?? cuota.FechaPago ?? DateTime.Now,
                SaldoInicial = saldoInicial,
                SaldoAnterior = saldoAnterior,
                Abono = abono,
                Recargo = recargo,
                SaldoActual = Math.Max(0, saldoActual),
                CI_RUC = cuota.Venta.Cliente.CI_RUC ?? "",
                UsuarioRegistro = venta.Vendedor?.UsuarioNombre ?? "Sistema"
            };

            var pdfBytes = TicketCuotaPdfService.Generar(data);
            var fileName = $"Ticket-Cuota{cuota.NumeroCuota}-Venta{venta.VentaId}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        private static NotaCreditoDto MapNotaCreditoDto(Models.NotaCredito n) => new()
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

        private static ConsignaContratoDto MapContratoDto(ContratoConsigna c) => new()
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
            PrecioVentaCliente = c.Tasacion?.ValorTasacion ?? c.Vehiculo.CostoAdquisicion ?? 0,
            PrecioVentaPlaya = c.Vehiculo.PrecioVenta
        };
    }
}
