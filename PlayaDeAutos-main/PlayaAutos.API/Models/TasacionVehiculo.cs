using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.API.Models
{
    public class TasacionVehiculo
    {
        [Key]
        public int TasacionVehiculoId { get; set; }
        public int ClienteId { get; set; }
        public DateTime FechaTasacion { get; set; } = DateTime.Now;
        public string MarcaVehiculo { get; set; } = string.Empty;
        public string ModeloVehiculo { get; set; } = string.Empty;
        public int AnhoVehiculo { get; set; }
        public long KilometrajeVehiculo { get; set; }    // ← long (antes decimal)
        public string EstadoGeneralVehiculo { get; set; } = string.Empty;
        public string? ColorVehiculo { get; set; }
        public long ValorTasacion { get; set; }
        public long PrecioVenta { get; set; }
        public string EstadoTasacion { get; set; } = "Pendiente";
        public int? ModeloId { get; set; }
        public int? TipoId { get; set; }
        public int? CondicionId { get; set; }
        public int? OrigenId { get; set; }
        public int? VehiculoId { get; set; }
        public int UsuarioTasacion { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public Cliente Cliente { get; set; } = null!;
        public Vehiculo? Vehiculo { get; set; }
        public ICollection<PagoVenta> PagosVenta { get; set; } = new List<PagoVenta>();
    }
}