/// Vehículo recomendado por el asistente (subconjunto de VehiculoDto del
/// catálogo: lo necesario para la tarjeta compacta del chat).
class VehiculoRecomendado {
  const VehiculoRecomendado({
    required this.id,
    required this.marca,
    required this.modelo,
    required this.anio,
    required this.precioVenta,
    this.kilometraje,
    this.transmision,
    this.combustible,
    this.fotoPrincipal,
  });

  final int id;
  final String marca;
  final String modelo;
  final int anio;
  final int precioVenta;
  final int? kilometraje;
  final String? transmision;
  final String? combustible;
  final String? fotoPrincipal;

  factory VehiculoRecomendado.fromJson(Map<String, dynamic> json) {
    return VehiculoRecomendado(
      id: json['vehiculoId'] as int,
      marca: json['marca'] as String? ?? '',
      modelo: json['modelo'] as String? ?? '',
      anio: json['anio'] as int? ?? 0,
      precioVenta: (json['precioVenta'] as num?)?.toInt() ?? 0,
      kilometraje: (json['kilometraje'] as num?)?.toInt(),
      transmision: json['transmision'] as String?,
      combustible: json['combustible'] as String?,
      fotoPrincipal: json['fotoPrincipal'] as String?,
    );
  }
}

/// Un mensaje de la conversación. Vive solo mientras el panel del asistente
/// está abierto: no se guarda historial.
class MensajeAsistente {
  const MensajeAsistente({
    required this.esUsuario,
    required this.texto,
    this.vehiculos = const [],
    this.esError = false,
  });

  final bool esUsuario;
  final String texto;
  final List<VehiculoRecomendado> vehiculos;
  final bool esError;

  /// Formato que espera la API en "historial".
  Map<String, String> toHistorial() => {
        'rol': esUsuario ? 'usuario' : 'asistente',
        'texto': texto,
      };
}
