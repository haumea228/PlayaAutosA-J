namespace PlayaAutos.API.Models
{
    public class Cliente
    {
        public int ClienteId { get; set; }
        public int? UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CI_RUC { get; set; }
        public string? Telefono { get; set; }   
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public long? RangoPrecioMin { get; set; }
        public long? RangoPrecioMax { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public Usuario? Usuario { get; set; }
        public Consignante? Consignante { get; set; }
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}