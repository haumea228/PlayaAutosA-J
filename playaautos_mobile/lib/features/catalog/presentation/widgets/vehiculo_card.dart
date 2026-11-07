import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/config/api_config.dart';
import '../../data/models/vehiculo_model.dart';

class VehiculoCard extends StatelessWidget {
  const VehiculoCard({super.key, required this.vehiculo});

  final VehiculoModel vehiculo;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final precio = NumberFormat.decimalPattern('es').format(vehiculo.precioVenta);
    final fotoUrl = vehiculo.fotoPrincipal;

    return Card(
      clipBehavior: Clip.antiAlias,
      margin: EdgeInsets.zero,
      child: InkWell(
        onTap: () => context.push('/catalogo/${vehiculo.id}'),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Stack(
              children: [
                AspectRatio(
                  aspectRatio: 4 / 3,
                  child: fotoUrl == null
                      ? Container(
                          color: colors.secondaryContainer,
                          child: Icon(
                            Icons.directions_car,
                            size: 40,
                            color: colors.onSecondaryContainer,
                          ),
                        )
                      : CachedNetworkImage(
                          imageUrl: '${ApiConfig.mediaBaseUrl}$fotoUrl',
                          fit: BoxFit.cover,
                          placeholder: (context, url) => Container(
                            color: colors.secondaryContainer,
                            child: const Center(
                              child: SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              ),
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
                Positioned(
                  right: 8,
                  top: 8,
                  child: _Pill(
                    text: '${vehiculo.anio}',
                    background: Colors.black.withValues(alpha: 0.55),
                    foreground: Colors.white,
                  ),
                ),
              ],
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(12, 10, 12, 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${vehiculo.marcaNombre} ${vehiculo.modeloNombre}',
                          style: Theme.of(context)
                              .textTheme
                              .titleSmall
                              ?.copyWith(fontWeight: FontWeight.bold),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 2),
                        Text(
                          vehiculo.tipoDescripcion,
                          style: Theme.of(context)
                              .textTheme
                              .bodySmall
                              ?.copyWith(color: colors.outline),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                    Text(
                      '₲ $precio',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: colors.primary,
                          ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  const _Pill({
    required this.text,
    required this.background,
    required this.foreground,
  });

  final String text;
  final Color background;
  final Color foreground;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        text,
        style: TextStyle(
          color: foreground,
          fontSize: 12,
          fontWeight: FontWeight.w500,
        ),
      ),
    );
  }
}
