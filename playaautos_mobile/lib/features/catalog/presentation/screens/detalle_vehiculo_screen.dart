import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/config/api_config.dart';
import '../../data/models/foto_vehiculo_model.dart';
import '../../data/models/vehiculo_model.dart';
import '../providers/catalogo_provider.dart';

class DetalleVehiculoScreen extends ConsumerWidget {
  const DetalleVehiculoScreen({super.key, required this.vehiculoId});

  final String vehiculoId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final id = int.tryParse(vehiculoId);
    if (id == null) {
      return const Scaffold(
        body: Center(child: Text('Vehículo inválido')),
      );
    }

    final vehiculoAsync = ref.watch(detalleVehiculoProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Detalle del vehículo')),
      body: vehiculoAsync.when(
        data: (vehiculo) => _DetalleContent(vehiculo: vehiculo),
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
                  'No se pudo cargar el vehículo.\n$error',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: () => ref.invalidate(detalleVehiculoProvider(id)),
                  child: const Text('Reintentar'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _DetalleContent extends StatelessWidget {
  const _DetalleContent({required this.vehiculo});

  final VehiculoModel vehiculo;

  @override
  Widget build(BuildContext context) {
    final formato = NumberFormat.decimalPattern('es');

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _GaleriaFotos(fotos: vehiculo.fotos),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${vehiculo.marcaNombre} ${vehiculo.modeloNombre}',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 4),
                Text(
                  '₲ ${formato.format(vehiculo.precioVenta)}',
                  style: Theme.of(context)
                      .textTheme
                      .titleLarge
                      ?.copyWith(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 16),
                _DatoFila(label: 'Año', valor: '${vehiculo.anio}'),
                _DatoFila(label: 'Color', valor: vehiculo.color ?? '—'),
                _DatoFila(
                  label: 'Kilometraje',
                  valor: vehiculo.kilometraje != null
                      ? '${formato.format(vehiculo.kilometraje)} km'
                      : '—',
                ),
                if (vehiculo.descripcion != null &&
                    vehiculo.descripcion!.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text(
                    'Descripción',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 4),
                  Text(vehiculo.descripcion!),
                ],
                const SizedBox(height: 24),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(
                    onPressed: () =>
                        context.push('/agendar-cita/${vehiculo.id}'),
                    child: const Text('Agendar cita'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Carrusel de fotos del vehículo con puntos indicadores de página abajo,
/// para que se note que hay más de una imagen para deslizar.
class _GaleriaFotos extends StatefulWidget {
  const _GaleriaFotos({required this.fotos});

  final List<FotoVehiculoModel> fotos;

  @override
  State<_GaleriaFotos> createState() => _GaleriaFotosState();
}

class _GaleriaFotosState extends State<_GaleriaFotos> {
  int _paginaActual = 0;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    if (widget.fotos.isEmpty) {
      return SizedBox(
        height: 260,
        child: Container(
          color: colors.secondaryContainer,
          child: Icon(
            Icons.directions_car,
            size: 64,
            color: colors.onSecondaryContainer,
          ),
        ),
      );
    }

    return SizedBox(
      height: 260,
      child: Stack(
        children: [
          PageView.builder(
            itemCount: widget.fotos.length,
            onPageChanged: (index) => setState(() => _paginaActual = index),
            itemBuilder: (context, index) => CachedNetworkImage(
              imageUrl:
                  '${ApiConfig.mediaBaseUrl}${widget.fotos[index].url}',
              fit: BoxFit.cover,
              placeholder: (context, url) => Container(
                color: colors.secondaryContainer,
                child: const Center(
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              ),
              errorWidget: (context, url, error) => Container(
                color: colors.secondaryContainer,
                child: Icon(
                  Icons.broken_image_outlined,
                  color: colors.onSecondaryContainer,
                ),
              ),
            ),
          ),
          if (widget.fotos.length > 1)
            Positioned(
              bottom: 12,
              left: 0,
              right: 0,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < widget.fotos.length; i++)
                    AnimatedContainer(
                      duration: const Duration(milliseconds: 200),
                      margin: const EdgeInsets.symmetric(horizontal: 3),
                      width: i == _paginaActual ? 18 : 7,
                      height: 7,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(
                          alpha: i == _paginaActual ? 0.95 : 0.5,
                        ),
                        borderRadius: BorderRadius.circular(4),
                        boxShadow: const [
                          BoxShadow(
                            color: Colors.black26,
                            blurRadius: 3,
                            offset: Offset(0, 1),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _DatoFila extends StatelessWidget {
  const _DatoFila({required this.label, required this.valor});

  final String label;
  final String valor;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
            child: Text(
              label,
              style: Theme.of(context)
                  .textTheme
                  .bodyMedium
                  ?.copyWith(color: Theme.of(context).colorScheme.outline),
            ),
          ),
          Expanded(child: Text(valor)),
        ],
      ),
    );
  }
}
