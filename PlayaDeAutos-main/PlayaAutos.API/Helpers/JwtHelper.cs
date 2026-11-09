using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PlayaAutos.API.Models;

namespace PlayaAutos.API.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerarToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Email, usuario.UsuarioEmail),
                new Claim(ClaimTypes.Name, usuario.UsuarioNombre),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.Add(Duracion(usuario.Rol)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Los clientes usan la app en su propio celular y esperan seguir con la
        /// sesión iniciada al volver a abrirla (sesión persistente): su token dura
        /// Jwt:DiasSesionCliente (30 días por defecto). El personal trabaja en la
        /// web, a veces en equipos compartidos: 8 horas, como siempre.
        /// </summary>
        private TimeSpan Duracion(string rol)
        {
            if (rol != "Cliente") return TimeSpan.FromHours(8);
            var dias = int.TryParse(_config["Jwt:DiasSesionCliente"], out var d) && d > 0 ? d : 30;
            return TimeSpan.FromDays(dias);
        }
    }
}