import 'foto_vehiculo_model.dart';

// Wire format verified against VehiculoDto.cs / VehiculosController.cs.
// El JSON real usa "marca"/"modelo"/"tipo"/"condicion" (no "marcaNombre" etc.
// como en el diccionario de datos) y "vehiculoId" en vez de "id" — fromJson
// traduce esos nombres a los campos Dart pedidos.
//
// Aviso: modeloId/tipoId/condicionId/estadoId siempre llegan en 0 desde los
// endpoints públicos (/catalogo y /catalogo/{id}); esos DTOs solo completan
// los IDs en los endpoints admin (/api/Vehiculos, /api/Vehiculos/{id}).
//
// fotoPrincipal se agregó además de los campos pedidos originalmente:
// GET /api/Vehiculos/catalogo (el listado) siempre devuelve "fotos": [] vacío
// — solo /catalogo/{id} (detalle) llena esa lista — así que sin este campo
// las cards del grid no tendrían ninguna imagen que mostrar. Es una ruta
// relativa (ver ApiConfig.mediaBaseUrl para armar la URL completa).
class VehiculoModel {
  const VehiculoModel({
    required this.id,
    required this.codigoInterno,
    required this.modeloId,
    required this.tipoId,
    required this.condicionId,
    required this.estadoId,
    required this.anio,
    this.color,
    this.kilometraje,
    required this.precioVenta,
    this.descripcion,
    required this.fotos,
    this.fotoPrincipal,
    required this.marcaNombre,
    required this.modeloNombre,
    required this.tipoDescripcion,
    required this.condicionDescripcion,
    this.transmision,
    this.combustible,
    this.cantidadPuertas,
    this.motor,
    this.traccion,
  });

  final int id;
  final String codigoInterno;
  final int modeloId;
  final int tipoId;
  final int condicionId;
  final int estadoId;
  final int anio;
  final String? color;
  final int? kilometraje;
  final int precioVenta;
  final String? descripcion;
  final List<FotoVehiculoModel> fotos;
  final String? fotoPrincipal;
  final String marcaNombre;
  final String modeloNombre;
  final String tipoDescripcion;
  final String condicionDescripcion;

  // Ficha técnica (columnas propias del vehículo en la API; pueden faltar en
  // vehículos cargados antes de que existieran).
  final String? transmision;
  final String? combustible;
  final int? cantidadPuertas;
  final String? motor;
  final String? traccion;

  bool get tieneFichaTecnica =>
      [transmision, combustible, motor, traccion].any((v) => v != null && v.isNotEmpty) ||
      cantidadPuertas != null;

  /// La API antepone a la descripción la línea "Ficha técnica: …" armada con
  /// los mismos datos de arriba. Cuando la app los muestra en filas propias,
  /// se quita esa línea para no repetirlos.
  String? get descripcionSinFicha {
    final texto = descripcion?.trim();
    if (texto == null || texto.isEmpty) return null;
    if (!tieneFichaTecnica || !texto.startsWith('Ficha técnica:')) return texto;
    final corte = texto.indexOf('\n\n');
    if (corte == -1) return null;
    final resto = texto.substring(corte).trim();
    return resto.isEmpty ? null : resto;
  }

  factory VehiculoModel.fromJson(Map<String, dynamic> json) {
    return VehiculoModel(
      id: json['vehiculoId'] as int,
      codigoInterno: json['codigoInterno'] as String? ?? '',
      modeloId: json['modeloId'] as int? ?? 0,
      tipoId: json['tipoId'] as int? ?? 0,
      condicionId: json['condicionId'] as int? ?? 0,
      estadoId: json['estadoId'] as int? ?? 0,
      anio: json['anio'] as int,
      color: json['color'] as String?,
      kilometraje: json['kilometraje'] as int?,
      precioVenta: json['precioVenta'] as int,
      descripcion: json['descripcion'] as String?,
      fotos: (json['fotos'] as List<dynamic>? ?? [])
          .map((e) => FotoVehiculoModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      fotoPrincipal: json['fotoPrincipal'] as String?,
      marcaNombre: json['marca'] as String? ?? '',
      modeloNombre: json['modelo'] as String? ?? '',
      tipoDescripcion: json['tipo'] as String? ?? '',
      condicionDescripcion: json['condicion'] as String? ?? '',
      transmision: json['transmision'] as String?,
      combustible: json['combustible'] as String?,
      cantidadPuertas: (json['cantidadPuertas'] as num?)?.toInt(),
      motor: json['motor'] as String?,
      traccion: json['traccion'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'vehiculoId': id,
      'codigoInterno': codigoInterno,
      'modeloId': modeloId,
      'tipoId': tipoId,
      'condicionId': condicionId,
      'estadoId': estadoId,
      'anio': anio,
      'color': color,
      'kilometraje': kilometraje,
      'precioVenta': precioVenta,
      'descripcion': descripcion,
      'fotos': fotos.map((f) => f.toJson()).toList(),
      'fotoPrincipal': fotoPrincipal,
      'marca': marcaNombre,
      'modelo': modeloNombre,
      'tipo': tipoDescripcion,
      'condicion': condicionDescripcion,
      'transmision': transmision,
      'combustible': combustible,
      'cantidadPuertas': cantidadPuertas,
      'motor': motor,
      'traccion': traccion,
    };
  }
}
