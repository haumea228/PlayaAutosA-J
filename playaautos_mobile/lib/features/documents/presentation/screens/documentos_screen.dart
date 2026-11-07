import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../config/theme/app_theme.dart';
import '../../../installments/data/models/cuota_model.dart';
import '../../data/contrato_pdf_builder.dart';
import '../../data/models/contrato_consigna_model.dart';
import '../../data/models/factura_model.dart';
import '../../data/models/nota_credito_model.dart';
import '../providers/documentos_provider.dart';
import '../widgets/documento_card.dart';

class DocumentosScreen extends ConsumerWidget {
  const DocumentosScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final facturasAsync = ref.watch(misFacturasProvider);
    final contratosAsync = ref.watch(misContratosProvider);
    final notasAsync = ref.watch(misNotasCreditoProvider);
    final ticketsAsync = ref.watch(misTicketsCuotaProvider);
    final repo = ref.watch(documentsRepositoryProvider);
    final formatoFecha = DateFormat('dd/MM/yyyy');
    final formatoMonto = NumberFormat.decimalPattern('es');

    return Scaffold(
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
            const Text('Mis Archivos'),
          ],
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _Seccion<FacturaModel>(
            icono: Icons.receipt_long_outlined,
            titulo: 'Facturas',
            async: facturasAsync,
            emptyMessage: 'Todavía no tenés facturas.',
            onRetry: () => ref.invalidate(misFacturasProvider),
            itemBuilder: (f) => DocumentoCard(
              icono: Icons.receipt_long_outlined,
              titulo: 'Factura ${f.numeroFactura}',
              subtitulo:
                  '${formatoFecha.format(f.fechaEmision)} · ₲ ${formatoMonto.format(f.total)}',
              nombreArchivo: 'Factura-${f.numeroFactura}',
              cargarPdf: () => repo.descargarFacturaPdf(f.id),
            ),
          ),
          _Seccion<ContratoConsignaModel>(
            icono: Icons.handshake_outlined,
            titulo: 'Contratos de consignación',
            async: contratosAsync,
            emptyMessage: 'Todavía no tenés contratos de consignación.',
            onRetry: () => ref.invalidate(misContratosProvider),
            itemBuilder: (c) => DocumentoCard(
              icono: Icons.handshake_outlined,
              titulo: 'Contrato ${c.numeroContrato}',
              subtitulo: '${c.vehiculoMarca} ${c.vehiculoModelo} · ${c.estado}',
              nombreArchivo: 'Contrato-${c.numeroContrato}',
              // Único de los 4 sin PDF en el backend: se genera acá mismo.
              cargarPdf: () => construirContratoPdf(c),
            ),
          ),
          _Seccion<NotaCreditoModel>(
            icono: Icons.assignment_return_outlined,
            titulo: 'Notas de crédito',
            async: notasAsync,
            emptyMessage: 'Todavía no tenés notas de crédito.',
            onRetry: () => ref.invalidate(misNotasCreditoProvider),
            itemBuilder: (n) => DocumentoCard(
              icono: Icons.assignment_return_outlined,
              titulo: 'Nota de crédito ${n.numeroNota}',
              subtitulo:
                  '${formatoFecha.format(n.fechaEmision)} · ₲ ${formatoMonto.format(n.monto)} · ${n.motivo}',
              nombreArchivo: 'NotaCredito-${n.numeroNota}',
              cargarPdf: () => repo.descargarNotaCreditoPdf(n.id),
            ),
          ),
          _Seccion<CuotaModel>(
            icono: Icons.confirmation_number_outlined,
            titulo: 'Tickets de pago de cuotas',
            async: ticketsAsync,
            emptyMessage: 'Todavía no tenés cuotas pagadas.',
            onRetry: () => ref.invalidate(misTicketsCuotaProvider),
            itemBuilder: (cuota) => DocumentoCard(
              icono: Icons.confirmation_number_outlined,
              titulo: 'Cuota N° ${cuota.numeroCuota}',
              subtitulo: cuota.fechaPago != null
                  ? '${cuota.vehiculo} · pagada el ${formatoFecha.format(cuota.fechaPago!)}'
                  : cuota.vehiculo,
              nombreArchivo: 'Ticket-Cuota${cuota.numeroCuota}',
              cargarPdf: () => repo.descargarTicketCuota(cuota.id),
            ),
          ),
        ],
      ),
    );
  }
}

/// Un acordeón: título + ícono siempre visibles, contenido (lista de
/// [DocumentoCard], estado vacío, loading o error con reintentar) solo al
/// expandir.
class _Seccion<T> extends StatelessWidget {
  const _Seccion({
    required this.icono,
    required this.titulo,
    required this.async,
    required this.itemBuilder,
    required this.emptyMessage,
    required this.onRetry,
  });

  final IconData icono;
  final String titulo;
  final AsyncValue<List<T>> async;
  final Widget Function(T item) itemBuilder;
  final String emptyMessage;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    return Card(
      clipBehavior: Clip.antiAlias,
      margin: const EdgeInsets.only(bottom: 12),
      child: ExpansionTile(
        leading: Icon(icono, color: colors.primary),
        title: Text(titulo),
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
            child: async.when(
              data: (items) {
                if (items.isEmpty) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    child: Text(
                      emptyMessage,
                      style: TextStyle(color: colors.outline),
                    ),
                  );
                }
                return Column(
                  children: [for (final item in items) itemBuilder(item)],
                );
              },
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, stack) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 12),
                child: Column(
                  children: [
                    Text(
                      'No se pudo cargar.\n$error',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton(
                      onPressed: onRetry,
                      child: const Text('Reintentar'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
