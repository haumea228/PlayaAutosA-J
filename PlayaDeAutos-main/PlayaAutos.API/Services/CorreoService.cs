using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace PlayaAutos.API.Services
{
    public class CorreoOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Puerto { get; set; } = 587;

        /// <summary>Cuenta que envía (Gmail de la empresa).</summary>
        public string Usuario { get; set; } = "ayjautosusados@gmail.com";

        /// <summary>
        /// Archivo con la contraseña de aplicación de Gmail (16 letras), relativo
        /// al ContentRoot. Fuera de git, como las demás claves de App_Data.
        /// </summary>
        public string PasswordPath { get; set; } = "App_Data/gmail-app-password.txt";

        public string NombreRemitente { get; set; } = "A&J Autos Usados";
    }

    /// <summary>
    /// Envío de correos por SMTP (Gmail de la empresa, gratis). Si falta el
    /// archivo con la contraseña de aplicación queda deshabilitado y la API
    /// funciona igual, sin poder enviar emails.
    /// </summary>
    public class CorreoService
    {
        private readonly CorreoOptions _opciones;
        private readonly string? _password;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IOptions<CorreoOptions> opciones, IWebHostEnvironment env, ILogger<CorreoService> logger)
        {
            _opciones = opciones.Value;
            _logger = logger;

            var ruta = Path.Combine(env.ContentRootPath, _opciones.PasswordPath);
            if (File.Exists(ruta))
            {
                // Google muestra la contraseña en 4 grupos con espacios: se quitan.
                var limpia = new string(File.ReadAllText(ruta).Where(c => !char.IsWhiteSpace(c)).ToArray());
                _password = limpia.Length > 0 ? limpia : null;
            }
            if (_password == null)
                _logger.LogWarning("Envío de emails deshabilitado: no se encontró {Ruta}.", ruta);
        }

        public bool Habilitado => _password != null;

        /// <summary>Envía un email HTML. Devuelve false si no se pudo enviar.</summary>
        public async Task<bool> EnviarAsync(string destinatario, string asunto, string html, string textoPlano)
        {
            if (_password == null) return false;

            try
            {
                using var mensaje = new MailMessage
                {
                    From = new MailAddress(_opciones.Usuario, _opciones.NombreRemitente),
                    Subject = asunto,
                    Body = textoPlano,
                    IsBodyHtml = false,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };
                mensaje.To.Add(destinatario);
                mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    html, System.Text.Encoding.UTF8, "text/html"));

                using var smtp = new SmtpClient(_opciones.Host, _opciones.Puerto)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_opciones.Usuario, _password),
                    Timeout = 20000
                };
                await smtp.SendMailAsync(mensaje);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el email '{Asunto}'.", asunto);
                return false;
            }
        }
    }
}
