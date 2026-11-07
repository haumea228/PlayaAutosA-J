using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.API.DTOs
{
    /// <summary>Token FCM del dispositivo (POST/DELETE api/micuenta/dispositivos).</summary>
    public class DispositivoDto
    {
        public string Token { get; set; } = string.Empty;
        public string? Plataforma { get; set; }
    }

    public class ClienteDto
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string CI_RUC { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
        public bool EsConsignante { get; set; }
        public int? ConsignanteId { get; set; }
        public decimal? PorcentajeComisionDefault { get; set; }
    }

    public class CrearClienteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string CI_RUC { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public long? RangoPrecioMin { get; set; }
        public long? RangoPrecioMax { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class ConvertirConsignanteDto
    {
        [Required]
        [Range(0.1, 100, ErrorMessage = "El porcentaje debe estar entre 0,1 y 100.")]
        public decimal PorcentajeComisionDefault { get; set; }
    }

    public class ActualizarPerfilClienteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
    }
}