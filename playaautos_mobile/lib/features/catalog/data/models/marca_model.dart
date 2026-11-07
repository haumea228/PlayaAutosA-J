// Aviso: GET /api/Marcas actualmente requiere rol AdministradorP/Vendedor
// (MarcasController.cs) — un Cliente no puede consultarlo. Hasta que se
// habilite un endpoint público, las opciones de filtro por marca deben
// derivarse de VehiculoModel.marcaNombre en la lista del catálogo.
class MarcaModel {
  const MarcaModel({required this.id, required this.nombre});

  final int id;
  final String nombre;

  factory MarcaModel.fromJson(Map<String, dynamic> json) {
    return MarcaModel(
      id: json['marcaId'] as int,
      nombre: json['nombre'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {'marcaId': id, 'nombre': nombre};
  }
}
