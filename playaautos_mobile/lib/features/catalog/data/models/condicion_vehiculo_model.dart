// Aviso: GET /api/Catalogos/condiciones-vehiculo también es AdministradorP/
// Vendedor/Cajero-only (CatalogosController.cs) — mismo caso que MarcaModel.
class CondicionVehiculoModel {
  const CondicionVehiculoModel({required this.id, required this.descripcion});

  final int id;
  final String descripcion;

  factory CondicionVehiculoModel.fromJson(Map<String, dynamic> json) {
    return CondicionVehiculoModel(
      id: json['condicionId'] as int,
      descripcion: json['descripcion'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {'condicionId': id, 'descripcion': descripcion};
  }
}
