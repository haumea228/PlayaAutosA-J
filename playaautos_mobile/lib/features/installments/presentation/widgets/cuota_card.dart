import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../shared/utils/formatters.dart';
import '../../../documents/presentation/providers/documentos_provider.dart';
import '../../../documents/presentation/widgets/documento_card.dart';
import '../../data/models/cuota_model.dart';

/// Estado visual de una cuota (texto, color e ícono del chip).
class EstadoCuotaVisual {
  const EstadoCuotaVisual(this.texto, this.color, this.icono);

  final String texto;
  final Color color;
  final IconData icono;

  static const Color _verde = Color(0xFF16A34A);
  static const Color _azul = Color(0xFF2563EB);
  static const Color _ambar = Color(0xFFCA8A04);
  static const Color _rojo = Color(0xFFDC2626);

  factory EstadoCuotaVisual.de(CuotaModel cuota, ColorScheme colors) {
    if (cuota.estaPagada) {
      return const EstadoCuotaVisual('Pagada', _verde, Icons.check_circle);
    }
    if (cuota.estaEnRevision) {
      return const EstadoCuotaVisual('En revisión', _azul, Icons.hourglass_top);
    }
    if (cuota.fueRechazada) {
      return const EstadoCuotaVisual('Rechazado', _rojo, Icons.cancel_outlined);
    }
    if (cuota.estaVencida) {
      return const EstadoCuotaVisual('Vencida', _rojo, Icons.warning_amber_rounded);
    }
    if (cuota.estaPendiente) {
      return const EstadoCuotaVisual('Pendiente', _ambar, Icons.schedule);
    }
    return EstadoCuotaVisual(cuota.estado, colors.outline, Icons.block);
  }
}

class EstadoCuotaChip extends StatelessWidget {
  const EstadoCuotaChip({super.key, required this.cuota});

  final CuotaModel cuota;

  @override
  Widget build(BuildContext context) {
    final v = EstadoCuotaVisual.de(cuota, Theme.of(context).colorScheme);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: v.color.withValues(alpha: 0.13),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(v.icono, size: 14, color: v.color),
          const SizedBox(width: 5),
          Text(
            v.texto,
            style: TextStyle(
              color: v.color,
              fontSize: 12,
              fontWeight: FontWeight.bold,
            ),
          ),
        ],
      ),
    );
  }
}

/// Tarjeta de una cuota. [destacada] = la próxima a pagar de su venta: se
/// muestra con el monto a transferir y el botón para informar el pago.
class CuotaCard extends ConsumerStatefulWidget {
  const CuotaCard({super.key, required this.cuota, this.destacada = false});

  final CuotaModel cuota;
  final bool destacada;

  @override
  ConsumerState<CuotaCard> createState() => _CuotaCardState();
}

class _CuotaCardState extends ConsumerState<CuotaCard> {
  bool _cargandoTicket = false;

  Future<void> _verTicket() async {
    final cuota = widget.cuota;
    setState(() => _cargandoTicket = true);
    try {
      final bytes = await ref
          .read(documentsRepositoryProvider)
          .descargarTicketCuota(cuota.id);
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => VisorPdfScreen(
            bytes: bytes,
            titulo: 'Ticket cuota N° ${cuota.numeroCuota}',
          ),
        ),
      );
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No se pudo obtener el ticket.')),
        );
      }
    } finally {
      if (mounted) setState(() => _cargandoTicket = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cuota = widget.cuota;
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final total = cuota.cantidadCuotas;
    final titulo = total != null
        ? 'Cuota ${cuota.numeroCuota} de $total'
        : 'Cuota N° ${cuota.numeroCuota}';

    return Card(
      margin: EdgeInsets.zero,
      elevation: widget.destacada ? 3 : 0.5,
      child: Padding(
        padding: EdgeInsets.all(widget.destacada ? 16 : 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    titulo,
                    style: (widget.destacada
                            ? theme.textTheme.titleMedium
                            : theme.textTheme.titleSmall)
                        ?.copyWith(fontWeight: FontWeight.bold),
                  ),
                ),
                EstadoCuotaChip(cuota: cuota),
              ],
            ),
            const SizedBox(height: 6),
            _Linea(
              icono: Icons.event_outlined,
              texto: 'Vence el ${Formatters.fecha(cuota.fechaVencimiento)}',
              color: cuota.estaVencida ? colors.error : colors.outline,
            ),
            if (cuota.estaPagada && cuota.fechaPago != null)
              _Linea(
                icono: Icons.check,
                texto: 'Pagada el ${Formatters.fecha(cuota.fechaPago!)}',
                color: colors.outline,
              ),
            const SizedBox(height: 8),
            _Montos(cuota: cuota, destacada: widget.destacada),
            if (cuota.estaEnRevision) ...[
              const SizedBox(height: 10),
              _Aviso(
                color: const Color(0xFF2563EB),
                icono: Icons.hourglass_top,
                texto: cuota.ultimaSolicitudFecha != null
                    ? 'Enviaste el comprobante el ${Formatters.fechaHora(cuota.ultimaSolicitudFecha!)}. '
                        'Cuando el encargado confirme la transferencia vas a poder descargar tu ticket.'
                    : 'Tu comprobante está siendo revisado.',
              ),
            ],
            if (cuota.fueRechazada) ...[
              const SizedBox(height: 10),
              _Aviso(
                color: colors.error,
                icono: Icons.info_outline,
                texto:
                    'Tu último comprobante fue rechazado: ${cuota.ultimaSolicitudMotivoRechazo ?? 'sin motivo informado'}. '
                    'Podés enviar uno nuevo.',
              ),
            ],
            if (widget.destacada &&
                cuota.estaPendiente &&
                !cuota.puedeInformarPago &&
                cuota.motivoNoPuedePagar != null) ...[
              const SizedBox(height: 10),
              Text(
                cuota.motivoNoPuedePagar!,
                style: theme.textTheme.bodySmall?.copyWith(color: colors.outline),
              ),
            ],
            if (cuota.puedeInformarPago && widget.destacada) ...[
              const SizedBox(height: 14),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () => context.push('/pagar-cuota/${cuota.id}'),
                  icon: const Icon(Icons.upload_file),
                  label: Text(
                    cuota.fueRechazada ? 'Volver a pagar' : 'Pagar cuota',
                  ),
                ),
              ),
            ],
            if (cuota.estaPagada) ...[
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerRight,
                child: _cargandoTicket
                    ? const Padding(
                        padding: EdgeInsets.all(8),
                        child: SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        ),
                      )
                    : TextButton.icon(
                        onPressed: _verTicket,
                        icon: const Icon(Icons.receipt_long_outlined),
                        label: const Text('Ver ticket'),
                      ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Montos extends StatelessWidget {
  const _Montos({required this.cuota, required this.destacada});

  final CuotaModel cuota;
  final bool destacada;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    if (cuota.estaPagada) {
      final pagado = (cuota.montoPagado ?? cuota.monto) + (cuota.montoRecargo ?? 0);
      return Text(
        Formatters.guaranies(pagado),
        style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
      );
    }

    final tieneRecargo = cuota.recargoCalculado > 0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (destacada)
          Text(
            'Monto a pagar',
            style: theme.textTheme.bodySmall?.copyWith(color: colors.outline),
          ),
        Text(
          Formatters.guaranies(cuota.montoAPagar),
          style: (destacada ? theme.textTheme.headlineSmall : theme.textTheme.titleMedium)
              ?.copyWith(fontWeight: FontWeight.bold, color: colors.primary),
        ),
        if (tieneRecargo)
          Text(
            'Cuota ${Formatters.guaranies(cuota.monto)} + recargo por mora ${Formatters.guaranies(cuota.recargoCalculado)}',
            style: theme.textTheme.bodySmall?.copyWith(color: colors.error),
          ),
      ],
    );
  }
}

class _Linea extends StatelessWidget {
  const _Linea({required this.icono, required this.texto, required this.color});

  final IconData icono;
  final String texto;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 2),
      child: Row(
        children: [
          Icon(icono, size: 14, color: color),
          const SizedBox(width: 6),
          Flexible(
            child: Text(
              texto,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: color),
            ),
          ),
        ],
      ),
    );
  }
}

class _Aviso extends StatelessWidget {
  const _Aviso({required this.color, required this.icono, required this.texto});

  final Color color;
  final IconData icono;
  final String texto;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icono, size: 18, color: color),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              texto,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: color),
            ),
          ),
        ],
      ),
    );
  }
}
