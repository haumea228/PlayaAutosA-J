// Aviso: no hay endpoint público/Cliente para listar EstadoCita — tanto
// GET /api/Citas/estados como GET /api/Catalogos/estados-cita son
// AdministradorP/Vendedor(/Cajero)-only. Para "mis citas" no hace falta:
// cada CitaModel ya trae su propio estadoDescripcion/estadoColor
// denormalizados. Este modelo queda listo por si se deriva del historial
// de citas del cliente o se habilita un endpoint público más adelante.
class EstadoCitaModel {
  const EstadoCitaModel({
    required this.id,
    required this.descripcion,
    this.color,
  });

  final int id;
  final String descripcion;
  final String? color;

  factory EstadoCitaModel.fromJson(Map<String, dynamic> json) {
    return EstadoCitaModel(
      id: json['estadoCitaId'] as int,
      descripcion: json['descripcion'] as String,
      color: json['color'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'estadoCitaId': id,
      'descripcion': descripcion,
      'color': color,
    };
  }
}
