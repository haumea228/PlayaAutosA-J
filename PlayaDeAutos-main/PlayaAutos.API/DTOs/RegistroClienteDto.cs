using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.API.DTOs
{
    public class RegistroClienteDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? Telefono { get; set; }
    }
}
