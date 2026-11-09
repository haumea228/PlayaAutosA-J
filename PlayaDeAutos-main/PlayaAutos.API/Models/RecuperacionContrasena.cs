namespace PlayaAutos.API.Models
{
    /// <summary>
    /// Código de 6 dígitos enviado por email para restablecer la contraseña
    /// ("¿Olvidaste tu contraseña?"). Solo se guarda su hash; vence a los
    /// pocos minutos, admite pocos intentos y se usa una sola vez.
    /// </summary>
    public class RecuperacionContrasena
    {
        public int RecuperacionContrasenaId { get; set; }
        public int UsuarioId { get; set; }
        public string CodigoHash { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public int Intentos { get; set; }

        /// <summary>Cuándo se usó (o se descartó por un código nuevo). Null = vigente.</summary>
        public DateTime? FechaUso { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}
