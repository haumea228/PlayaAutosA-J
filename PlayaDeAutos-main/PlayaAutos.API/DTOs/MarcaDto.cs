namespace PlayaAutos.API.DTOs
{
    public class MarcaDto
    {
        public int MarcaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class CrearMarcaDto
    {
        public string Nombre { get; set; } = string.Empty;
    }
}