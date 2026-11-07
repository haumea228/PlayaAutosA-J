// No existe ContratoConsignaDto en el backend — el contrato de consigna no
// genera PDF del lado del servidor (único de los 4 documentos de Mis
// Archivos en esa situación). Este modelo sigue el shape real de
// ConsignaContratoDto.cs (GET /api/micuenta/contratos), que trae los datos
// estructurados del contrato; el PDF se arma del lado del cliente a partir
// de estos campos (ver contrato_pdf_builder.dart).
class ContratoConsignaModel {
  const ContratoConsignaModel({
    required this.id,
    required this.numeroContrato,
    required this.fechaInicio,
    required this.fechaFin,
    required this.duracionDias,
    required this.porcentajeComision,
    required this.estado,
    this.clausulas,
    required this.fechaGeneracion,
    required this.consignanteNombre,
    this.consignanteCI,
    required this.vehiculoMarca,
    required this.vehiculoModelo,
    required this.vehiculoAnio,
    this.vehiculoColor,
    this.vehiculoKilometraje,
    this.vehiculoTipo,
    required this.precioVentaCliente,
    required this.precioVentaPlaya,
  });

  final int id;
  final String numeroContrato;
  final DateTime fechaInicio;
  final DateTime fechaFin;
  final int duracionDias;
  final double porcentajeComision;
  final String estado;
  final String? clausulas;
  final DateTime fechaGeneracion;
  final String consignanteNombre;
  final String? consignanteCI;
  final String vehiculoMarca;
  final String vehiculoModelo;
  final int vehiculoAnio;
  final String? vehiculoColor;
  final int? vehiculoKilometraje;
  final String? vehiculoTipo;
  final int precioVentaCliente;
  final int precioVentaPlaya;

  factory ContratoConsignaModel.fromJson(Map<String, dynamic> json) {
    return ContratoConsignaModel(
      id: json['contratoId'] as int,
      numeroContrato: json['numeroContrato'] as String? ?? '',
      fechaInicio: DateTime.parse(json['fechaInicio'] as String),
      fechaFin: DateTime.parse(json['fechaFin'] as String),
      duracionDias: json['duracionDias'] as int? ?? 0,
      porcentajeComision: (json['porcentajeComision'] as num?)?.toDouble() ?? 0,
      estado: json['estado'] as String? ?? '',
      clausulas: json['clausulas'] as String?,
      fechaGeneracion: DateTime.parse(json['fechaGeneracion'] as String),
      consignanteNombre: json['consignanteNombre'] as String? ?? '',
      consignanteCI: json['consignanteCI'] as String?,
      vehiculoMarca: json['vehiculoMarca'] as String? ?? '',
      vehiculoModelo: json['vehiculoModelo'] as String? ?? '',
      vehiculoAnio: json['vehiculoAnio'] as int? ?? 0,
      vehiculoColor: json['vehiculoColor'] as String?,
      vehiculoKilometraje: json['vehiculoKilometraje'] as int?,
      vehiculoTipo: json['vehiculoTipo'] as String?,
      precioVentaCliente: json['precioVentaCliente'] as int? ?? 0,
      precioVentaPlaya: json['precioVentaPlaya'] as int? ?? 0,
    );
  }
}
