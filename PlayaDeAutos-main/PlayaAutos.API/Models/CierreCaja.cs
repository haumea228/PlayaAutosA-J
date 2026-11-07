using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayaAutos.API.Models
{
    public class CierreCaja
    {
        [Key]
        public int CierreId { get; set; }

        public DateTime FechaCierre { get; set; }

        public int UsuarioCierre { get; set; }

        public long TotalIngresos { get; set; }

        public long TotalEgresos { get; set; }

        public long SaldoNeto { get; set; }

        public int CantidadMovimientos { get; set; }

        [StringLength(500)]
        public string? Observacion { get; set; }

        // Relaciones
        public Usuario Usuario { get; set; } = null!;
        public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
    }
}