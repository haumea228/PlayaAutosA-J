namespace PlayaAutos.API.DTOs
{
    public class CrearConsignaDto
    {
        public int ConsignanteId { get; set; }
        public int TasacionVehiculoId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal PorcentajeComision { get; set; }
        public string? Clausulas { get; set; }
    }

    public class ConsignaListDto
    {
        public int ContratoId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public int ConsignanteId { get; set; }
        public string Consignante { get; set; } = string.Empty;
        public string ConsignanteCI { get; set; } = string.Empty;
        public int VehiculoId { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal PorcentajeComision { get; set; }
        public long PrecioCliente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
    }

    public class ConsignanteListDto
    {
        public int ConsignanteId { get; set; }
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CI_RUC { get; set; }
        public decimal PorcentajeComisionDefault { get; set; }
    }

    public class TasacionParaConsignaDto
    {
        public int TasacionVehiculoId { get; set; }
        public int? VehiculoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public long PrecioVentaCliente { get; set; }
        public int? ModeloId { get; set; }
        public int? TipoId { get; set; }
        public int? CondicionId { get; set; }
        public int? OrigenId { get; set; }
        public string? Color { get; set; }
        public long Kilometraje { get; set; }
        public int Anio { get; set; }
    }

    public class ConsignaContratoDto
    {
        public int ContratoId { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DuracionDias { get; set; }
        public decimal PorcentajeComision { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Clausulas { get; set; }
        public DateTime FechaGeneracion { get; set; }

        public string ConsignanteNombre { get; set; } = string.Empty;
        public string? ConsignanteCI { get; set; }

        public string VehiculoMarca { get; set; } = string.Empty;
        public string VehiculoModelo { get; set; } = string.Empty;
        public int VehiculoAnio { get; set; }
        public string? VehiculoColor { get; set; }
        public long? VehiculoKilometraje { get; set; }
        public string? VehiculoTipo { get; set; }
        public long PrecioVentaCliente { get; set; }
        public long PrecioVentaPlaya { get; set; }
    }
}
