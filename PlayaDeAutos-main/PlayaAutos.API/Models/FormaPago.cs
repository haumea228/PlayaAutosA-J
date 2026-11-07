namespace PlayaAutos.API.Models
{
    public class FormaPago
    {
        public int FormaPagoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool GeneraNotaCredito { get; set; } = false;

        public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
        public ICollection<PagoVenta> PagosVenta { get; set; } = new List<PagoVenta>();
    }
}