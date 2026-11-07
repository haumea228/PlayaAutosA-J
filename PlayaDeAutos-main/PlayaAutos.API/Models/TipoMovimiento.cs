namespace PlayaAutos.API.Models
{
    public class TipoMovimiento
    {
        public int TipoMovimientoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Signo { get; set; } = string.Empty;
        public bool AfectaCaja { get; set; } = true;

        public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = new List<MovimientoCaja>();
    }
}