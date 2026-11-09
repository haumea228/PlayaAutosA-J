using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthController(AppDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
        {
            var passwordHash = HashPassword(dto.Password);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.UsuarioEmail == dto.Email &&
                    u.PasswordHash == passwordHash &&
                    u.ActivoUsuario);

            if (usuario == null)
                return Unauthorized("Email o contraseña incorrectos.");

            var token = _jwtHelper.GenerarToken(usuario);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Nombre = usuario.UsuarioNombre,
                Rol = usuario.Rol,
                UsuarioId = usuario.UsuarioId
            });
        }

        // POST: api/auth/registro — crea un usuario AdministradorP. Solo un
        // administrador con sesión iniciada puede usarlo (antes era público y
        // cualquiera podía crearse una cuenta de administrador). El primer
        // admin lo crea la API al arrancar con la base vacía (Program.cs), y
        // el resto del personal se da de alta desde la web (api/usuarios).
        [HttpPost("registro")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "AdministradorP")]
        public async Task<IActionResult> Registro(LoginDto dto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.UsuarioEmail == dto.Email))
                return BadRequest("El email ya está registrado.");

            var usuario = new Models.Usuario
            {
                UsuarioNombre = dto.Email,
                UsuarioEmail = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Rol = "AdministradorP",
                FechaAltaUsuario = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok("Usuario creado correctamente.");
        }

        // POST: api/auth/registro-cliente
        [HttpPost("registro-cliente")]
        public async Task<IActionResult> RegistroCliente(RegistroClienteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Usuarios.AnyAsync(u => u.UsuarioEmail == dto.Email))
                return BadRequest("El email ya está registrado.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuario = new Models.Usuario
                {
                    UsuarioNombre = dto.Nombre,
                    UsuarioEmail = dto.Email,
                    UsuarioPhone = dto.Telefono,
                    PasswordHash = HashPassword(dto.Password),
                    Rol = "Cliente",
                    ActivoUsuario = true,
                    FechaAltaUsuario = DateTime.Now
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                var cliente = new Models.Cliente
                {
                    UsuarioId = usuario.UsuarioId,
                    Nombre = dto.Nombre,
                    Telefono = dto.Telefono,
                    Email = dto.Email,
                    Activo = true,
                    FechaAlta = DateTime.Now
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok("Cuenta creada exitosamente.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error al crear la cuenta. Intentá nuevamente.");
            }
        }

        public const string PoliticaRecuperacion = "recuperacion-contrasena";
        private const string MensajeCodigoEnviado =
            "Si el email corresponde a una cuenta, te enviamos un código de 6 dígitos. Revisá tu correo (también la carpeta de spam).";

        // POST: api/auth/olvide-contrasena — envía un código por email.
        // Responde siempre lo mismo para no revelar qué emails tienen cuenta.
        [HttpPost("olvide-contrasena")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(PoliticaRecuperacion)]
        public async Task<IActionResult> OlvideContrasena(OlvideContrasenaDto dto,
            [FromServices] Services.RecuperacionContrasenaService recuperacion)
        {
            if (!recuperacion.Habilitado)
                return StatusCode(503, "La recuperación de contraseña no está disponible por ahora. Comunicate con la playa.");

            await recuperacion.SolicitarAsync(dto.Email);
            return Ok(new { mensaje = MensajeCodigoEnviado });
        }

        // POST: api/auth/restablecer-contrasena — código + nueva contraseña.
        [HttpPost("restablecer-contrasena")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(PoliticaRecuperacion)]
        public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaDto dto,
            [FromServices] Services.RecuperacionContrasenaService recuperacion)
        {
            var error = Services.RecuperacionContrasenaService.ValidarPassword(dto.NuevaPassword);
            if (error != null) return BadRequest(error);

            return await recuperacion.RestablecerAsync(dto.Email, dto.Codigo, dto.NuevaPassword) switch
            {
                Services.ResultadoRestablecer.Ok => Ok(new { mensaje = "Listo, ya podés iniciar sesión con tu nueva contraseña." }),
                Services.ResultadoRestablecer.DemasiadosIntentos => BadRequest("Hiciste demasiados intentos con ese código. Pedí uno nuevo."),
                _ => BadRequest("El código no es correcto o ya venció.")
            };
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
