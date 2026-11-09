using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;
using PlayaAutos.API.Helpers;
using PlayaAutos.API.Models;

namespace PlayaAutos.API.Services
{
    public enum ResultadoRestablecer { Ok, CodigoInvalido, DemasiadosIntentos, PasswordInvalida }

    /// <summary>
    /// "¿Olvidaste tu contraseña?" con código de 6 dígitos por email:
    /// - el código vence a los <see cref="MinutosVigencia"/> minutos y admite
    ///   <see cref="MaxIntentos"/> intentos; se guarda solo su hash;
    /// - pedir uno nuevo anula el anterior;
    /// - la respuesta al pedirlo es siempre la misma, exista o no la cuenta,
    ///   para no revelar qué emails están registrados.
    /// </summary>
    public class RecuperacionContrasenaService
    {
        public const int MinutosVigencia = 15;
        public const int MaxIntentos = 5;
        public const int LargoMinimoPassword = 6;
        private static readonly TimeSpan EsperaEntreCodigos = TimeSpan.FromSeconds(60);

        private readonly AppDbContext _context;
        private readonly CorreoService _correo;
        private readonly ILogger<RecuperacionContrasenaService> _logger;

        public RecuperacionContrasenaService(AppDbContext context, CorreoService correo,
            ILogger<RecuperacionContrasenaService> logger)
        {
            _context = context;
            _correo = correo;
            _logger = logger;
        }

        public bool Habilitado => _correo.Habilitado;

        public static string? ValidarPassword(string? password) =>
            string.IsNullOrEmpty(password) || password.Length < LargoMinimoPassword
                ? $"La contraseña debe tener al menos {LargoMinimoPassword} caracteres."
                : password.Length > 100 ? "La contraseña es demasiado larga." : null;

        /// <summary>Genera y envía un código si el email corresponde a un cliente activo.</summary>
        public async Task SolicitarAsync(string? email)
        {
            email = email?.Trim();
            if (string.IsNullOrEmpty(email)) return;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.UsuarioEmail == email && u.ActivoUsuario && u.Rol == "Cliente");
            if (usuario == null) return;

            var ahora = DateTime.Now;
            var vigentes = await _context.RecuperacionesContrasena
                .Where(r => r.UsuarioId == usuario.UsuarioId && r.FechaUso == null)
                .ToListAsync();

            // Evita usar la API para llenar de emails una casilla ajena.
            if (vigentes.Any(r => ahora - r.FechaCreacion < EsperaEntreCodigos)) return;

            foreach (var anterior in vigentes) anterior.FechaUso = ahora;

            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _context.RecuperacionesContrasena.Add(new RecuperacionContrasena
            {
                UsuarioId = usuario.UsuarioId,
                CodigoHash = HashCodigo(codigo, usuario.UsuarioId),
                FechaCreacion = ahora,
                FechaExpiracion = ahora.AddMinutes(MinutosVigencia)
            });
            await _context.SaveChangesAsync();

            var (html, texto) = Mensaje(usuario.UsuarioNombre, codigo);
            var enviado = await _correo.EnviarAsync(usuario.UsuarioEmail,
                $"{codigo} es tu código para restablecer la contraseña", html, texto);
            if (!enviado)
                _logger.LogWarning("No se envió el código de recuperación al usuario {UsuarioId}.", usuario.UsuarioId);
        }

        public async Task<ResultadoRestablecer> RestablecerAsync(string? email, string? codigo, string? nuevaPassword)
        {
            if (ValidarPassword(nuevaPassword) != null) return ResultadoRestablecer.PasswordInvalida;

            email = email?.Trim();
            codigo = codigo?.Trim();
            if (string.IsNullOrEmpty(email) || codigo is not { Length: 6 } || !codigo.All(char.IsDigit))
                return ResultadoRestablecer.CodigoInvalido;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.UsuarioEmail == email && u.ActivoUsuario && u.Rol == "Cliente");
            if (usuario == null) return ResultadoRestablecer.CodigoInvalido;

            var ahora = DateTime.Now;
            var solicitud = await _context.RecuperacionesContrasena
                .Where(r => r.UsuarioId == usuario.UsuarioId && r.FechaUso == null && r.FechaExpiracion > ahora)
                .OrderByDescending(r => r.FechaCreacion)
                .FirstOrDefaultAsync();
            if (solicitud == null) return ResultadoRestablecer.CodigoInvalido;
            if (solicitud.Intentos >= MaxIntentos) return ResultadoRestablecer.DemasiadosIntentos;

            var esperado = Encoding.UTF8.GetBytes(solicitud.CodigoHash);
            var recibido = Encoding.UTF8.GetBytes(HashCodigo(codigo, usuario.UsuarioId));
            if (!CryptographicOperations.FixedTimeEquals(esperado, recibido))
            {
                solicitud.Intentos++;
                await _context.SaveChangesAsync();
                return solicitud.Intentos >= MaxIntentos
                    ? ResultadoRestablecer.DemasiadosIntentos
                    : ResultadoRestablecer.CodigoInvalido;
            }

            usuario.PasswordHash = Contrasenas.Hash(nuevaPassword!);
            solicitud.FechaUso = ahora;
            await _context.SaveChangesAsync();
            return ResultadoRestablecer.Ok;
        }

        /// <summary>Hash del código atado al usuario (el mismo código no sirve para otra cuenta).</summary>
        private static string HashCodigo(string codigo, int usuarioId) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{usuarioId}:{codigo}"))).ToLower();

        private static (string Html, string Texto) Mensaje(string nombre, string codigo)
        {
            var n = WebUtility.HtmlEncode(nombre);
            var texto =
                $"Hola {nombre}:\n\n" +
                $"Tu código para restablecer la contraseña de la app de A&J Autos Usados es: {codigo}\n\n" +
                $"Vence en {MinutosVigencia} minutos. Si no lo pediste, ignorá este mensaje: tu contraseña no cambia.\n\n" +
                "A&J Autos Usados - Carapeguá";
            var html = $@"<div style=""font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#0C2E3D"">
  <h2 style=""margin-bottom:4px"">A&amp;J Autos Usados</h2>
  <p>Hola {n}:</p>
  <p>Tu código para restablecer la contraseña de la app es:</p>
  <p style=""font-size:32px;font-weight:bold;letter-spacing:8px;color:#186784;margin:16px 0"">{codigo}</p>
  <p>Vence en {MinutosVigencia} minutos. Ingresalo en la app junto con tu nueva contraseña.</p>
  <p style=""color:#9A7C65;font-size:13px"">Si no pediste este código, ignorá este mensaje: tu contraseña no cambia.</p>
</div>";
            return (html, texto);
        }
    }
}
