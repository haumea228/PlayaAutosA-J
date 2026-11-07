namespace PlayaAutos.API.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string? UsuarioPhone { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool ActivoUsuario { get; set; } = true;
        public DateTime FechaAltaUsuario { get; set; } = DateTime.Now;

        public Cliente? Cliente { get; set; }
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}