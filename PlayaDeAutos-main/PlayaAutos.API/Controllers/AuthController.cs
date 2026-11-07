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

        // POST: api/auth/registro (solo para crear el primer admin)
        [HttpPost("registro")]
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

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
