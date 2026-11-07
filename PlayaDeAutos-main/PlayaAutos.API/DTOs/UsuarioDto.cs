namespace PlayaAutos.API.DTOs
{
    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string? UsuarioPhone { get; set; }
        public string Rol { get; set; } = string.Empty;
        public bool ActivoUsuario { get; set; }
        public DateTime FechaAltaUsuario { get; set; }
    }

    public class CrearUsuarioDto
    {
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string? UsuarioPhone { get; set; }
        public string Rol { get; set; } = "Vendedor";
        public string Password { get; set; } = string.Empty;
    }

    public class ActualizarUsuarioDto
    {
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string? UsuarioPhone { get; set; }
        public string Rol { get; set; } = string.Empty;
        public bool ActivoUsuario { get; set; }
        public string? NuevaPassword { get; set; }
    }
}