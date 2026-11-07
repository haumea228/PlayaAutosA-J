namespace PlayaAutos.API.Models
{
    public class TipoVenta
    {
        public int TipoVentaId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool RequiereFinanciacion { get; set; } = false;

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}