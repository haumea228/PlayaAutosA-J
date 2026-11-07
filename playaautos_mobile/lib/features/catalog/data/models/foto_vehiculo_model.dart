class FotoVehiculoModel {
  const FotoVehiculoModel({
    required this.id,
    required this.url,
    this.nombreArchivo,
    required this.esPrincipal,
    required this.orden,
  });

  final int id;
  final String url;
  final String? nombreArchivo;
  final bool esPrincipal;
  final int orden;

  factory FotoVehiculoModel.fromJson(Map<String, dynamic> json) {
    return FotoVehiculoModel(
      id: json['fotoId'] as int,
      url: json['url'] as String,
      nombreArchivo: json['nombreArchivo'] as String?,
      esPrincipal: json['esPrincipal'] as bool,
      orden: json['orden'] as int,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'fotoId': id,
      'url': url,
      'nombreArchivo': nombreArchivo,
      'esPrincipal': esPrincipal,
      'orden': orden,
    };
  }
}
