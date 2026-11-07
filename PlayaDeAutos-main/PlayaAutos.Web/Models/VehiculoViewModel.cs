using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PlayaAutos.Web.Models
{
    public class VehiculoViewModel
    {
        [Required(ErrorMessage = "El código interno es obligatorio.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [Display(Name = "Código Interno")]
        public string CodigoInterno { get; set; } = "";

        [Required(ErrorMessage = "Seleccione una marca.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una marca válida.")]
        [Display(Name = "Marca")]
        public int MarcaId { get; set; }

        [Required(ErrorMessage = "Seleccione un modelo.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un modelo válido.")]
        [Display(Name = "Modelo")]
        public int ModeloId { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de vehículo.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tipo válido.")]
        [Display(Name = "Tipo")]
        public int TipoId { get; set; }

        [Required(ErrorMessage = "Seleccione la condición.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una condición válida.")]
        [Display(Name = "Condición")]
        public int CondicionId { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estado válido.")]
        [Display(Name = "Estado")]
        public int EstadoId { get; set; }

        [Required(ErrorMessage = "Seleccione el origen.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un origen válido.")]
        [Display(Name = "Origen")]
        public int OrigenId { get; set; }

        [Required(ErrorMessage = "El año es obligatorio.")]
        [Range(1900, 2030, ErrorMessage = "Ingrese un año válido entre 1900 y 2030.")]
        [Display(Name = "Año")]
        public int Anio { get; set; } = DateTime.Now.Year;

        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        [Range(0, 9_999_999, ErrorMessage = "El kilometraje no puede ser negativo.")]
        [Display(Name = "Kilometraje")]
        public long? Kilometraje { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(1, long.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        [Display(Name = "Precio de Venta (Gs.)")]
        public long PrecioVenta { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "El costo de adquisición no puede ser negativo.")]
        [Display(Name = "Costo de Adquisición (Gs.)")]
        public long? CostoAdquisicion { get; set; }

        [StringLength(1000, ErrorMessage = "Máximo 1000 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        // ── Ficha técnica (opcional; la API valida los valores permitidos) ──
        [Display(Name = "Transmisión")]
        public string? Transmision { get; set; }

        [Display(Name = "Combustible")]
        public string? Combustible { get; set; }

        [Range(2, 5, ErrorMessage = "Entre 2 y 5 puertas.")]
        [Display(Name = "Cantidad de puertas")]
        public int? CantidadPuertas { get; set; }

        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        [Display(Name = "Motor")]
        public string? Motor { get; set; }

        [Display(Name = "Tracción")]
        public string? Traccion { get; set; }

        [ValidateNever]
        [Display(Name = "Fotos")]
        public List<IFormFile>? Fotos { get; set; }

        // Listas para los selects — no se validan en POST (se cargan server-side)
        [ValidateNever] public List<SelectListItem> Marcas { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Modelos { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Tipos { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Condiciones { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Estados { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Origenes { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Transmisiones { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Combustibles { get; set; } = new();
        [ValidateNever] public List<SelectListItem> Tracciones { get; set; } = new();

        // Campos de edición — no se validan en POST
        public int VehiculoId { get; set; }
        [ValidateNever] public List<FotoExistenteItem> FotosExistentes { get; set; } = new();
        public string? FotoIdsEliminar { get; set; }
    }

    public class FotoExistenteItem
    {
        public int FotoId { get; set; }
        public string URL { get; set; } = "";
        public string NombreArchivo { get; set; } = "";
        public bool EsPrincipal { get; set; }
    }

    // DTO de solo lectura para la lista de vehículos
    public class VehiculoListItem
    {
        public int VehiculoId { get; set; }
        public string CodigoInterno { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Condicion { get; set; } = "";
        public string Estado { get; set; } = "";
        public int Anio { get; set; }
        public string? Color { get; set; }
        public long? Kilometraje { get; set; }
        public long PrecioVenta { get; set; }
        public string? Descripcion { get; set; }
        public string? FotoPrincipal { get; set; }
    }
}