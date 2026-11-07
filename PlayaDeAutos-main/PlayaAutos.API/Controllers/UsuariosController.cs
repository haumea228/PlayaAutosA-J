using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace PlayaAutos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "AdministradorP")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios()
        {
            return await _context.Usuarios
                .Select(u => new UsuarioDto
                {
                    UsuarioId = u.UsuarioId,
                    UsuarioNombre = u.UsuarioNombre,
                    UsuarioEmail = u.UsuarioEmail,
                    UsuarioPhone = u.UsuarioPhone,
                    Rol = u.Rol,
                    ActivoUsuario = u.ActivoUsuario,
                    FechaAltaUsuario = u.FechaAltaUsuario
                })
                .ToListAsync();
        }

        // GET: api/usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDto>> GetUsuario(int id)
        {
            var u = await _context.Usuarios.FindAsync(id);
            if (u == null) return NotFound();

            return new UsuarioDto
            {
                UsuarioId = u.UsuarioId,
                UsuarioNombre = u.UsuarioNombre,
                UsuarioEmail = u.UsuarioEmail,
                UsuarioPhone = u.UsuarioPhone,
                Rol = u.Rol,
                ActivoUsuario = u.ActivoUsuario,
                FechaAltaUsuario = u.FechaAltaUsuario
            };
        }

        // GET: api/usuarios/vendedores  (para dropdowns en otros módulos)
        [HttpGet("vendedores")]
        [Authorize(Roles = "AdministradorP,Vendedor")]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetVendedores()
        {
            return await _context.Usuarios
                .Where(u => (u.Rol == "Vendedor" || u.Rol == "AdministradorP") && u.ActivoUsuario)
                .Select(u => new UsuarioDto
                {
                    UsuarioId = u.UsuarioId,
                    UsuarioNombre = u.UsuarioNombre,
                    UsuarioEmail = u.UsuarioEmail,
                    Rol = u.Rol,
                    ActivoUsuario = u.ActivoUsuario
                })
                .ToListAsync();
        }

        // POST: api/usuarios
        [HttpPost]
        public async Task<IActionResult> PostUsuario(CrearUsuarioDto dto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.UsuarioEmail == dto.UsuarioEmail))
                return BadRequest("Ya existe un usuario registrado con ese email.");

            var usuario = new Usuario
            {
                UsuarioNombre = dto.UsuarioNombre,
                UsuarioEmail = dto.UsuarioEmail,
                UsuarioPhone = dto.UsuarioPhone,
                Rol = dto.Rol,
                PasswordHash = HashPassword(dto.Password),
                ActivoUsuario = true,
                FechaAltaUsuario = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario),
                new { id = usuario.UsuarioId },
                new { usuario.UsuarioId });
        }

        // PUT: api/usuarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, ActualizarUsuarioDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Verificar email duplicado (excluyendo el propio)
            if (await _context.Usuarios.AnyAsync(u => u.UsuarioEmail == dto.UsuarioEmail && u.UsuarioId != id))
                return BadRequest("Ya existe un usuario con ese email.");

            usuario.UsuarioNombre = dto.UsuarioNombre;
            usuario.UsuarioEmail = dto.UsuarioEmail;
            usuario.UsuarioPhone = dto.UsuarioPhone;
            usuario.Rol = dto.Rol;
            usuario.ActivoUsuario = dto.ActivoUsuario;

            // Cambiar contraseña solo si se envió una nueva
            if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
                usuario.PasswordHash = HashPassword(dto.NuevaPassword);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/usuarios/5/toggle-activo
        [HttpPut("{id}/toggle-activo")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.ActivoUsuario = !usuario.ActivoUsuario;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Helper ───────────────────────────────────────────────────────
        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}