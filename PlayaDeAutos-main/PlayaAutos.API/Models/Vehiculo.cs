namespace PlayaAutos.API.Models
{
    public class Vehiculo
    {
        public int VehiculoId { get; set; }
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

        // Ficha técnica (opcionales: los vehículos cargados antes quedan en null).
        // Valores permitidos en Helpers/FichaTecnica.
        public string? Transmision { get; set; }
        public string? Combustible { get; set; }
        public int? CantidadPuertas { get; set; }
        public string? Motor { get; set; }
        public string? Traccion { get; set; }

        public int? ConsignanteId { get; set; }
        public decimal? PorcentajeComision { get; set; }
        public DateTime? FechaConsigna { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.Now;
        public int UsuarioAlta { get; set; }

        public Modelo Modelo { get; set; } = null!;
        public TipoVehiculo Tipo { get; set; } = null!;
        public CondicionVehiculo Condicion { get; set; } = null!;
        public EstadoVehiculo Estado { get; set; } = null!;
        public OrigenVehiculo Origen { get; set; } = null!;
        public Consignante? Consignante { get; set; }

        public ICollection<FotoVehiculo> Fotos { get; set; } = new List<FotoVehiculo>();
        public ICollection<GastoVehiculo> Gastos { get; set; } = new List<GastoVehiculo>();
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<ContratoConsigna> ContratosConsigna { get; set; } = new List<ContratoConsigna>();
    }
}