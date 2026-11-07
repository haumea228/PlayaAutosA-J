namespace PlayaAutos.Web.Models
{
    public class MarcasModelosViewModel
    {
        public List<MarcaItem> Marcas { get; set; } = new();
        public List<ModeloItem> Modelos { get; set; } = new();
    }

    public class MarcaItem
    {
        public int MarcaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class ModeloItem
    {
        public int ModeloId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int MarcaId { get; set; }
        public string Marca { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}