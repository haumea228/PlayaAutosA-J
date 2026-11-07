// Aviso: GET /api/Catalogos/tipos-vehiculo también es AdministradorP/
// Vendedor/Cajero-only (CatalogosController.cs) — mismo caso que MarcaModel.
class TipoVehiculoModel {
  const TipoVehiculoModel({required this.id, required this.descripcion});

  final int id;
  final String descripcion;

  factory TipoVehiculoModel.fromJson(Map<String, dynamic> json) {
    return TipoVehiculoModel(
      id: json['tipoId'] as int,
      descripcion: json['descripcion'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {'tipoId': id, 'descripcion': descripcion};
  }
}
