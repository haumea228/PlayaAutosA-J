using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class UsuarioViewModel
    {
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre de usuario")]
        public string UsuarioNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
        [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
        [Display(Name = "Email")]
        public string UsuarioEmail { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
        [Display(Name = "Teléfono")]
        public string? UsuarioPhone { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; } = "Vendedor";

        [Display(Name = "Activo")]
        public bool ActivoUsuario { get; set; } = true;

        // Solo requerida al crear
        [Display(Name = "Contraseña")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? Password { get; set; }

        // Solo para editar: cambiar contraseña es opcional
        [Display(Name = "Nueva contraseña")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? NuevaPassword { get; set; }
    }

    public class UsuarioListItem
    {
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string? UsuarioPhone { get; set; }
        public string Rol { get; set; } = string.Empty;
        public bool ActivoUsuario { get; set; }
        public DateTime FechaAltaUsuario { get; set; }
    }
}