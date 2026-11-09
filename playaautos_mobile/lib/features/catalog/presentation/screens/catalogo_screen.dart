import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../config/theme/app_theme.dart';
import '../../../assistant/presentation/widgets/asistente_sheet.dart';
import '../../data/models/vehiculo_model.dart';
import '../providers/catalogo_provider.dart';
import '../widgets/filtro_catalogo.dart';
import '../widgets/vehiculo_card.dart';

class CatalogoScreen extends ConsumerStatefulWidget {
  const CatalogoScreen({super.key});

  @override
  ConsumerState<CatalogoScreen> createState() => _CatalogoScreenState();
}

class _CatalogoScreenState extends ConsumerState<CatalogoScreen> {
  final _busquedaController = TextEditingController();

  @override
  void dispose() {
    _busquedaController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final catalogoAsync = ref.watch(catalogoProvider);
    final filtro = ref.watch(filtroProvider);

    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        heroTag: 'asistente',
        onPressed: () => mostrarAsistente(context),
        icon: const Icon(Icons.auto_awesome),
        label: const Text('Asistente'),
      ),
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              width: 38,
              height: 38,
              padding: const EdgeInsets.all(5),
              decoration: const BoxDecoration(
                color: AppColors.offWhiteBeige,
                shape: BoxShape.circle,
              ),
              child: Image.asset(
                'assets/images/logo.png',
                fit: BoxFit.contain,
                cacheWidth: 100,
              ),
            ),
            const SizedBox(width: 10),
            const Text('Catálogo'),
          ],
        ),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            child: TextField(
              controller: _busquedaController,
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                hintText: 'Buscar por marca, modelo o año...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: filtro.busqueda != null && filtro.busqueda!.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.close),
                        onPressed: () {
                          _busquedaController.clear();
                          ref.read(filtroProvider.notifier).state =
                              filtro.copyWith(clearBusqueda: true);
                        },
                      )
                    : null,
              ),
              onChanged: (value) => ref.read(filtroProvider.notifier).state =
                  filtro.copyWith(busqueda: value, clearBusqueda: value.isEmpty),
            ),
          ),
          const FiltroCatalogoWidget(),
          Expanded(
            child: catalogoAsync.when(
              data: (vehiculos) {
                final filtrados = _aplicarFiltro(vehiculos, filtro);

                if (filtrados.isEmpty) {
                  return const Center(
                    child: Padding(
                      padding: EdgeInsets.all(24),
                      child: Text(
                        'No se encontraron vehículos con esos filtros.',
                        textAlign: TextAlign.center,
                      ),
                    ),
                  );
                }

                return RefreshIndicator(
                  onRefresh: () => ref.refresh(catalogoProvider.future),
                  child: GridView.builder(
                    padding: const EdgeInsets.all(12),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      mainAxisSpacing: 12,
                      crossAxisSpacing: 12,
                      childAspectRatio: 0.78,
                    ),
                    itemCount: filtrados.length,
                    itemBuilder: (context, index) =>
                        VehiculoCard(vehiculo: filtrados[index]),
                  ),
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, stack) => Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.error_outline, size: 40),
                      const SizedBox(height: 12),
                      Text(
                        'No se pudo cargar el catálogo.\n$error',
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 12),
                      FilledButton(
                        onPressed: () => ref.invalidate(catalogoProvider),
                        child: const Text('Reintentar'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  List<VehiculoModel> _aplicarFiltro(
    List<VehiculoModel> vehiculos,
    FiltroCatalogo filtro,
  ) {
    return vehiculos.where((v) {
      if (filtro.busqueda != null && filtro.busqueda!.trim().isNotEmpty) {
        final query = filtro.busqueda!.trim().toLowerCase();
        final texto =
            '${v.marcaNombre} ${v.modeloNombre} ${v.anio}'.toLowerCase();
        if (!texto.contains(query)) return false;
      }
      if (filtro.marca != null && v.marcaNombre != filtro.marca) return false;
      if (filtro.modelo != null && v.modeloNombre != filtro.modelo) return false;
      if (filtro.tipo != null && v.tipoDescripcion != filtro.tipo) return false;
      if (filtro.precioMin != null && v.precioVenta < filtro.precioMin!) {
        return false;
      }
      if (filtro.precioMax != null && v.precioVenta > filtro.precioMax!) {
        return false;
      }
      return true;
    }).toList();
  }
}
