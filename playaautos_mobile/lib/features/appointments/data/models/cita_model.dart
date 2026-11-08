// Wire format verified against CitaDto.cs / MiCuentaController.GetMisCitas().
// El JSON real usa "citaId" (no "id") y "vehiculo"/"estado" (strings ya
// formateados por el backend, no "vehiculoNombre"/"estadoDescripcion") —
// fromJson traduce esos nombres a los campos Dart pedidos.
class CitaModel {
  const CitaModel({
    required this.id,
    required this.clienteId,
    this.vehiculoId,
    required this.vendedorId,
    required this.estadoCitaId,
    required this.fechaHora,
    required this.tipoCita,
    this.observaciones,
    this.vehiculoNombre,
    required this.estadoDescripcion,
    this.estadoColor,
  });

  final int id;
  final int clienteId;
  final int? vehiculoId;
  final int vendedorId;
  final int estadoCitaId;
  final DateTime fechaHora;
  final String tipoCita;
  final String? observaciones;
  final String? vehiculoNombre;
  final String estadoDescripcion;
  final String? estadoColor;

  // Ids de la tabla EstadosCita: 1 Pendiente, 2 Confirmada, 3 Realizada, 4 Cancelada.
  /// Mismas reglas que la API: Pendiente o Confirmada, y con horario futuro.
  bool get puedeCancelarse =>
      (estadoCitaId == 1 || estadoCitaId == 2) &&
      fechaHora.isAfter(DateTime.now());

  factory CitaModel.fromJson(Map<String, dynamic> json) {
    return CitaModel(
      id: json['citaId'] as int,
      clienteId: json['clienteId'] as int,
      vehiculoId: json['vehiculoId'] as int?,
      vendedorId: json['vendedorId'] as int,
      estadoCitaId: json['estadoCitaId'] as int,
      fechaHora: DateTime.parse(json['fechaHora'] as String),
      tipoCita: json['tipoCita'] as String? ?? '',
      observaciones: json['observaciones'] as String?,
      vehiculoNombre: json['vehiculo'] as String?,
      estadoDescripcion: json['estado'] as String? ?? '',
      estadoColor: json['estadoColor'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'citaId': id,
      'clienteId': clienteId,
      'vehiculoId': vehiculoId,
      'vendedorId': vendedorId,
      'estadoCitaId': estadoCitaId,
      'fechaHora': fechaHora.toIso8601String(),
      'tipoCita': tipoCita,
      'observaciones': observaciones,
      'vehiculo': vehiculoNombre,
      'estado': estadoDescripcion,
      'estadoColor': estadoColor,
    };
  }
}
