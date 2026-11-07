import '../../../core/network/dio_client.dart';
import 'models/marca_model.dart';
import 'models/modelo_model.dart';
import 'models/vehiculo_model.dart';

class CatalogRepository {
  Future<List<VehiculoModel>> getCatalogo() async {
    final response = await DioClient.get('Vehiculos/catalogo');
    final list = response.data as List<dynamic>;
    return list
        .map((e) => VehiculoModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<VehiculoModel> getVehiculo(int id) async {
    // GET /api/Vehiculos/{id} es AdministradorP/Vendedor-only (verificado en
    // VehiculosController.cs); el equivalente público para un cliente es
    // /api/Vehiculos/catalogo/{id}.
    final response = await DioClient.get('Vehiculos/catalogo/$id');
    return VehiculoModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// GET /api/Marcas es AdministradorP/Vendedor-only (403 para un Cliente),
  /// así que las marcas se derivan del catálogo público en vez de llamar a
  /// ese endpoint. El id es un hash estable del nombre, no el MarcaId real
  /// (que tampoco viaja en el DTO público — ver nota en vehiculo_model.dart).
  Future<List<MarcaModel>> getMarcas() async {
    final vehiculos = await getCatalogo();
    final nombres = vehiculos
        .map((v) => v.marcaNombre)
        .where((n) => n.isNotEmpty)
        .toSet()
        .toList()
      ..sort();
    return nombres
        .map((nombre) => MarcaModel(id: nombre.hashCode, nombre: nombre))
        .toList();
  }

  /// Mismo criterio que getMarcas(): deriva los modelos de esa marca a
  /// partir del catálogo público, matcheando por el hash de marcaNombre que
  /// devuelve getMarcas().
  Future<List<ModeloModel>> getModelos(int marcaId) async {
    final vehiculos = await getCatalogo();
    final nombres = vehiculos
        .where((v) => v.marcaNombre.hashCode == marcaId)
        .map((v) => v.modeloNombre)
        .where((n) => n.isNotEmpty)
        .toSet()
        .toList()
      ..sort();
    return nombres
        .map((nombre) =>
            ModeloModel(id: nombre.hashCode, marcaId: marcaId, nombre: nombre))
        .toList();
  }
}
