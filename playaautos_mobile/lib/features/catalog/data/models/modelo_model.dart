// Aviso: GET /api/Modelos también es AdministradorP/Vendedor-only
// (ModelosController.cs) — mismo caso que MarcaModel.
class ModeloModel {
  const ModeloModel({
    required this.id,
    required this.marcaId,
    required this.nombre,
  });

  final int id;
  final int marcaId;
  final String nombre;

  factory ModeloModel.fromJson(Map<String, dynamic> json) {
    return ModeloModel(
      id: json['modeloId'] as int,
      marcaId: json['marcaId'] as int,
      nombre: json['nombre'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {'modeloId': id, 'marcaId': marcaId, 'nombre': nombre};
  }
}
