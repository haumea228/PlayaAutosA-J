import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/catalog_repository.dart';
import '../../data/models/marca_model.dart';
import '../../data/models/vehiculo_model.dart';

final catalogRepositoryProvider = Provider<CatalogRepository>((ref) {
  return CatalogRepository();
});

final catalogoProvider = FutureProvider<List<VehiculoModel>>((ref) {
  return ref.watch(catalogRepositoryProvider).getCatalogo();
});

final detalleVehiculoProvider =
    FutureProvider.family<VehiculoModel, int>((ref, id) {
  return ref.watch(catalogRepositoryProvider).getVehiculo(id);
});

final marcasProvider = FutureProvider<List<MarcaModel>>((ref) {
  return ref.watch(catalogRepositoryProvider).getMarcas();
});

/// Estado de los filtros del catálogo. Se filtra por nombre (marca/tipo) en
/// vez de id porque esos ids son derivados (ver CatalogRepository.getMarcas)
/// y no existen en VehiculoModel como valores comparables reales.
class FiltroCatalogo {
  const FiltroCatalogo({
    this.busqueda,
    this.marca,
    this.modelo,
    this.tipo,
    this.precioMin,
    this.precioMax,
  });

  final String? busqueda;
  final String? marca;

  /// Solo tiene sentido con una marca elegida: al cambiar la marca se limpia.
  final String? modelo;
  final String? tipo;
  final int? precioMin;
  final int? precioMax;

  FiltroCatalogo copyWith({
    String? busqueda,
    String? marca,
    String? modelo,
    String? tipo,
    int? precioMin,
    int? precioMax,
    bool clearBusqueda = false,
    bool clearMarca = false,
    bool clearModelo = false,
    bool clearTipo = false,
  }) {
    return FiltroCatalogo(
      busqueda: clearBusqueda ? null : (busqueda ?? this.busqueda),
      marca: clearMarca ? null : (marca ?? this.marca),
      modelo: clearModelo ? null : (modelo ?? this.modelo),
      tipo: clearTipo ? null : (tipo ?? this.tipo),
      precioMin: precioMin ?? this.precioMin,
      precioMax: precioMax ?? this.precioMax,
    );
  }
}

final filtroProvider =
    StateProvider<FiltroCatalogo>((ref) => const FiltroCatalogo());
