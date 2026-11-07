namespace PlayaAutos.Web.Models
{
    public class CatalogoItemViewModel
    {
        public int VehiculoId { get; set; }
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Anio { get; set; }
        public string? Color { get; set; }
        public long? Kilometraje { get; set; }
        public long PrecioVenta { get; set; }
        public string Tipo { get; set; } = "";
        public string Condicion { get; set; } = "";
        public string? FotoPrincipal { get; set; }
        public string? Descripcion { get; set; }
    }

    public class CatalogoDetalleViewModel
    {
        public int VehiculoId { get; set; }
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Anio { get; set; }
        public string? Color { get; set; }
        public long? Kilometraje { get; set; }
        public long PrecioVenta { get; set; }
        public string Tipo { get; set; } = "";
        public string Condicion { get; set; } = "";
        public string Estado { get; set; } = "";
        public string? FotoPrincipal { get; set; }
        public string? Descripcion { get; set; }
        public List<CatalogoFotoViewModel> Fotos { get; set; } = new();
    }

    public class CatalogoFotoViewModel
    {
        public string URL { get; set; } = "";
        public bool EsPrincipal { get; set; }
        public int Orden { get; set; }
    }
}
