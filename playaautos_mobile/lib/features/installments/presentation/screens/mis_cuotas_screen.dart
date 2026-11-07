import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/models/cuota_model.dart';
import '../providers/cuotas_provider.dart';
import '../widgets/cuota_card.dart';

class MisCuotasScreen extends ConsumerWidget {
  const MisCuotasScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cuotasAsync = ref.watch(misCuotasProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Mis Cuotas')),
      body: cuotasAsync.when(
        data: (cuotas) {
          final ventas = _agruparPorVenta(cuotas);

          return RefreshIndicator(
            onRefresh: () => ref.refresh(misCuotasProvider.future),
            child: ventas.isEmpty
                ? ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: const [SizedBox(height: 120), _SinCuotas()],
                  )
                : ListView.separated(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    itemCount: ventas.length,
                    separatorBuilder: (context, index) => const SizedBox(height: 16),
                    itemBuilder: (context, index) =>
                        _VentaCuotas(cuotas: ventas[index]),
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
                  'No se pudieron cargar tus cuotas.\n$error',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: () => ref.invalidate(misCuotasProvider),
                  child: const Text('Reintentar'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  /// Agrupa por venta (un vehículo financiado) y ordena cada grupo por número
  /// de cuota. Las ventas con algo por pagar van primero.
  List<List<CuotaModel>> _agruparPorVenta(List<CuotaModel> cuotas) {
    final grupos = <int, List<CuotaModel>>{};
    for (final c in cuotas.where((c) => c.estado != 'Anulada')) {
      grupos.putIfAbsent(c.ventaId, () => []).add(c);
    }
    final lista = grupos.values.toList();
    for (final g in lista) {
      g.sort((a, b) => a.numeroCuota.compareTo(b.numeroCuota));
    }
    lista.sort((a, b) {
      final aCompleta = a.every((c) => c.estaPagada);
      final bCompleta = b.every((c) => c.estaPagada);
      if (aCompleta != bCompleta) return aCompleta ? 1 : -1;
      return a.first.ventaId.compareTo(b.first.ventaId);
    });
    return lista;
  }
}

class _VentaCuotas extends StatelessWidget {
  const _VentaCuotas({required this.cuotas});

  final List<CuotaModel> cuotas;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final primera = cuotas.first;
    final total = primera.cantidadCuotas ?? cuotas.length;
    final pagadas = cuotas.where((c) => c.estaPagada).length;
    final completa = pagadas == cuotas.length;

    // Próxima cuota: la que está en revisión o la primera pendiente.
    final proxima = cuotas.cast<CuotaModel?>().firstWhere(
          (c) => c!.estaEnRevision || c.estaPendiente,
          orElse: () => null,
        );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            CircleAvatar(
              backgroundColor: colors.primary.withValues(alpha: 0.12),
              child: Icon(Icons.directions_car, color: colors.primary),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    primera.vehiculo,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  Text(
                    completa
                        ? '¡Terminaste de pagar! $pagadas de $total cuotas'
                        : '$pagadas de $total cuotas pagadas',
                    style: theme.textTheme.bodySmall
                        ?.copyWith(color: colors.outline),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        ClipRRect(
          borderRadius: BorderRadius.circular(999),
          child: LinearProgressIndicator(
            value: total == 0 ? 0 : pagadas / total,
            minHeight: 6,
            backgroundColor: colors.outline.withValues(alpha: 0.2),
          ),
        ),
        const SizedBox(height: 12),
        if (proxima != null) ...[
          Text(
            proxima.estaEnRevision ? 'Pago en revisión' : 'Próxima cuota',
            style: theme.textTheme.labelLarge?.copyWith(color: colors.outline),
          ),
          const SizedBox(height: 6),
          CuotaCard(cuota: proxima, destacada: true),
          const SizedBox(height: 8),
        ],
        Card(
          margin: EdgeInsets.zero,
          elevation: 0.5,
          child: ExpansionTile(
            leading: const Icon(Icons.list_alt),
            title: Text('Ver todas las cuotas (${cuotas.length})'),
            childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
            children: [
              for (final c in cuotas) ...[
                CuotaCard(cuota: c),
                const SizedBox(height: 8),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _SinCuotas extends StatelessWidget {
  const _SinCuotas();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          Icon(
            Icons.payments_outlined,
            size: 48,
            color: Theme.of(context).colorScheme.outline,
          ),
          const SizedBox(height: 12),
          const Text(
            'No tenés cuotas registradas.\nSi compraste un vehículo a crédito, tus cuotas van a aparecer acá.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
