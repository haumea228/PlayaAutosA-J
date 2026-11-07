namespace PlayaAutos.API.DTOs
{
    public class VehiculoDto
    {
        public int VehiculoId { get; set; }
        public string CodigoInterno { get; set; } = string.Empty;
        // IDs para pre-popular dropdowns en edición
        public int MarcaId { get; set; }
        public int ModeloId { get; set; }
        public int TipoId { get; set; }
        public int CondicionId { get; set; }
        public int EstadoId { get; set; }
        public int OrigenId { get; set; }
        // Nombres descriptivos
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Condicion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string? Color { get; set; }
        public long? Kilometraje { get; set; }
        public long PrecioVenta { get; set; }
        public long? CostoAdquisicion { get; set; }
        public string? Descripcion { get; set; }
        public string? Transmision { get; set; }
        public string? Combustible { get; set; }
        public int? CantidadPuertas { get; set; }
        public string? Motor { get; set; }
        public string? Traccion { get; set; }
        public string? FotoPrincipal { get; set; }
        public List<FotoVehiculoDto> Fotos { get; set; } = new();
    }

    public class CrearVehiculoDto
    {
        public string CodigoInterno { get; set; } = string.Empty;
        public int ModeloId { get; set; }
        public int TipoId { get; set; }
        public int CondicionId { get; set; }
        public int EstadoId { get; set; }
        public int OrigenId { get; set; }
        public int Anio { get; set; }
        public string? Color { get; set; }
        public long? Kilometraje { get; set; }
        public long PrecioVenta { get; set; }
        public long? CostoAdquisicion { get; set; }
        public string? Descripcion { get; set; }
        public string? Transmision { get; set; }
        public string? Combustible { get; set; }
        public int? CantidadPuertas { get; set; }
        public string? Motor { get; set; }
        public string? Traccion { get; set; }
    }

    public class FotoVehiculoDto
    {
        public int FotoId { get; set; }
        public string URL { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public bool EsPrincipal { get; set; }
        public int Orden { get; set; }
    }

    public class GuardarFotoDto
    {
        public string URL { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public bool EsPrincipal { get; set; }
        public int Orden { get; set; }
    }
}