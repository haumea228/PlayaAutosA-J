namespace PlayaAutos.API.Models
{
    /// <summary>
    /// Token de Firebase Cloud Messaging de un dispositivo donde el usuario
    /// (cliente) inició sesión en la app. Un usuario puede tener varios
    /// dispositivos; un token pertenece a un solo usuario a la vez.
    /// </summary>
    public class DispositivoNotificacion
    {
        public int DispositivoNotificacionId { get; set; }
        public int UsuarioId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string? Plataforma { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaUltimoUso { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}
