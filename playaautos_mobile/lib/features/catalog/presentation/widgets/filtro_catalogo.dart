import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../providers/catalogo_provider.dart';

// Nombrado FiltroCatalogoWidget (no FiltroCatalogo) para no chocar con la
// clase de estado FiltroCatalogo de catalogo_provider.dart, que es el tipo
// pedido explícitamente para filtroProvider.
class FiltroCatalogoWidget extends ConsumerWidget {
  const FiltroCatalogoWidget({super.key});

  // Tope superior para el slider de precio; no hay un máximo real conocido
  // del catálogo en este punto, así que se usa un valor razonable en guaraníes.
  static const double _precioMax = 200000000;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final filtro = ref.watch(filtroProvider);
    final marcasAsync = ref.watch(marcasProvider);
    final catalogoAsync = ref.watch(catalogoProvider);

    // No hay endpoint público de tipos de vehículo (ver tipo_vehiculo_model.dart),
    // así que se derivan del catálogo ya cargado, igual que las marcas.
    final tipos = catalogoAsync.maybeWhen(
      data: (vehiculos) => (vehiculos
              .map((v) => v.tipoDescripcion)
              .where((t) => t.isNotEmpty)
              .toSet()
              .toList()
            ..sort()),
      orElse: () => const <String>[],
    );

    void actualizar(FiltroCatalogo Function(FiltroCatalogo) update) {
      ref.read(filtroProvider.notifier).state = update(filtro);
    }

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: marcasAsync.when(
                  data: (marcas) => DropdownButtonFormField<String>(
                    initialValue: filtro.marca,
                    decoration: const InputDecoration(labelText: 'Marca'),
                    items: [
                      const DropdownMenuItem<String>(child: Text('Todas')),
                      for (final marca in marcas)
                        DropdownMenuItem(
                          value: marca.nombre,
                          child: Text(marca.nombre),
                        ),
                    ],
                    onChanged: (value) => actualizar(
                      (f) => f.copyWith(marca: value, clearMarca: value == null),
                    ),
                  ),
                  loading: () => const LinearProgressIndicator(),
                  error: (error, stack) => const SizedBox.shrink(),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: DropdownButtonFormField<String>(
                  initialValue: filtro.tipo,
                  decoration: const InputDecoration(labelText: 'Tipo'),
                  items: [
                    const DropdownMenuItem<String>(child: Text('Todos')),
                    for (final tipo in tipos)
                      DropdownMenuItem(value: tipo, child: Text(tipo)),
                  ],
                  onChanged: (value) => actualizar(
                    (f) => f.copyWith(tipo: value, clearTipo: value == null),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text('Rango de precio (₲)', style: Theme.of(context).textTheme.labelMedium),
          RangeSlider(
            min: 0,
            max: _precioMax,
            divisions: 20,
            labels: RangeLabels(
              '₲ ${((filtro.precioMin ?? 0) / 1000000).round()}M',
              '₲ ${((filtro.precioMax ?? _precioMax) / 1000000).round()}M',
            ),
            values: RangeValues(
              (filtro.precioMin ?? 0).toDouble(),
              (filtro.precioMax ?? _precioMax).toDouble(),
            ),
            onChanged: (values) => actualizar(
              (f) => f.copyWith(
                precioMin: values.start.round(),
                precioMax: values.end.round(),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
