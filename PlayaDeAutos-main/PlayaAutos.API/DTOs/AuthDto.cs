namespace PlayaAutos.API.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
    }

    /// <summary>"¿Olvidaste tu contraseña?": pide el código por email.</summary>
    public class OlvideContrasenaDto
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Restablece la contraseña con el código recibido por email.</summary>
    public class RestablecerContrasenaDto
    {
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }

    /// <summary>Cambio de contraseña con la sesión iniciada (desde Perfil).</summary>
    public class CambiarContrasenaDto
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }
}