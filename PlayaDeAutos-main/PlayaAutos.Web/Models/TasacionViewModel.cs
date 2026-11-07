using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class ModeloSelectItem
    {
        public int ModeloId { get; set; }
        public string NombreModelo { get; set; } = string.Empty;
        public string NombreMarca { get; set; } = string.Empty;

    }

    public class TasacionViewModel
    {

        [Display(Name = "Vehículo del catálogo")]
        public int? VehiculoId { get; set; }

        [Required(ErrorMessage = "El cliente es obligatorio.")]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "Seleccione un modelo del catálogo.")]
        [Display(Name = "Marca")]
        public string MarcaVehiculo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione un modelo del catálogo.")]
        [Display(Name = "Modelo")]
        public string ModeloVehiculo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El año es obligatorio.")]
        [Range(1900, 2100)]
        [Display(Name = "Año")]
        public int AnhoVehiculo { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "El kilometraje es obligatorio.")]
        [Range(0, 9999999)]
        [Display(Name = "Kilometraje")]
        public long KilometrajeVehiculo { get; set; }        // ← long

        [Required(ErrorMessage = "El estado general es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Estado general")]
        public string EstadoGeneralVehiculo { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Color")]
        public string? ColorVehiculo { get; set; }

        [Required(ErrorMessage = "El valor de tasación es obligatorio.")]
        [Range(1, long.MaxValue)]
        [Display(Name = "Valor tasación (Gs.)")]
        public long ValorTasacion { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(1, long.MaxValue)]
        [Display(Name = "Precio venta (Gs.)")]
        public long PrecioVenta { get; set; }

        [Display(Name = "Modelo (catálogo)")]
        public int? ModeloId { get; set; }

        [Display(Name = "Tipo de vehículo")]
        public int? TipoId { get; set; }

        [Display(Name = "Condición")]
        public int? CondicionId { get; set; }

        [Display(Name = "Origen")]
        public int? OrigenId { get; set; }

        public int TasacionVehiculoId { get; set; }
        public string EstadoTasacion { get; set; } = "Pendiente";
        public DateTime FechaTasacion { get; set; }

        public List<SelectListItem> Clientes { get; set; } = new();
        public List<ModeloSelectItem> ModelosConMarca { get; set; } = new();
        public List<SelectListItem> Tipos { get; set; } = new();
        public List<SelectListItem> Condiciones { get; set; } = new();
        public List<SelectListItem> Origenes { get; set; } = new();
        public List<SelectListItem> VehiculosCatalogo { get; set; } = new();
    }

    public class AprobarTasacionViewModel
    {
        public int TasacionVehiculoId { get; set; }

        public string ClienteNombre { get; set; } = string.Empty;
        public string MarcaVehiculo { get; set; } = string.Empty;
        public string ModeloVehiculo { get; set; } = string.Empty;
        public int AnhoVehiculo { get; set; }
        public long KilometrajeVehiculo { get; set; }        // ← long
        public string EstadoGeneralVehiculo { get; set; } = string.Empty;
        public string? ColorVehiculo { get; set; }

        [Required(ErrorMessage = "El valor de tasación final es obligatorio.")]
        [Range(1, long.MaxValue)]
        [Display(Name = "Valor tasación final (Gs.)")]
        public long ValorTasacionFinal { get; set; }

        [Required(ErrorMessage = "El precio de venta final es obligatorio.")]
        [Range(1, long.MaxValue)]
        [Display(Name = "Precio venta final (Gs.)")]
        public long PrecioVentaFinal { get; set; }

        [Display(Name = "Modelo")]
        public int ModeloId { get; set; }

        [Display(Name = "Tipo")]
        public int TipoId { get; set; }

        [Display(Name = "Condición")]
        public int CondicionId { get; set; }

        [Display(Name = "Origen")]
        public int OrigenId { get; set; }

        [Display(Name = "Color")]
        public string? Color { get; set; }

        [Required(ErrorMessage = "El kilometraje es obligatorio.")]
        [Range(0, long.MaxValue)]
        [Display(Name = "Kilometraje")]
        public long Kilometraje { get; set; }

        public List<ModeloSelectItem> ModelosConMarca { get; set; } = new();
        public List<SelectListItem> Tipos { get; set; } = new();
        public List<SelectListItem> Condiciones { get; set; } = new();
        public List<SelectListItem> Origenes { get; set; } = new();
    }

    public class TasacionListItem
    {
        public int TasacionVehiculoId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string MarcaVehiculo { get; set; } = string.Empty;
        public string ModeloVehiculo { get; set; } = string.Empty;
        public int AnhoVehiculo { get; set; }
        public long ValorTasacion { get; set; }
        public long PrecioVenta { get; set; }
        public string EstadoTasacion { get; set; } = string.Empty;
        public DateTime FechaTasacion { get; set; }
    }
}