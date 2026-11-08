import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../data/models/cita_model.dart';
import '../providers/citas_provider.dart';

/// Tarjeta de una cita. Si tiene descripción (observaciones del cliente o el
/// motivo de cancelación que escribe el encargado), al tocarla se despliega
/// completa; contraída muestra solo las primeras 2 líneas.
///
/// Si la cita todavía se puede cancelar (Pendiente o Confirmada y con
/// horario futuro), muestra el botón "Cancelar cita".
class CitaCard extends ConsumerStatefulWidget {
  const CitaCard({super.key, required this.cita});

  final CitaModel cita;

  @override
  ConsumerState<CitaCard> createState() => _CitaCardState();
}

class _CitaCardState extends ConsumerState<CitaCard> {
  bool _expandida = false;
  bool _cancelando = false;

  Future<void> _cancelar() async {
    final motivo = await showDialog<String>(
      context: context,
      builder: (_) => _DialogoCancelar(cita: cita),
    );
    if (motivo == null || !mounted) return; // el usuario se arrepintió

    setState(() => _cancelando = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref
          .read(appointmentsRepositoryProvider)
          .cancelarCita(cita.id, motivo: motivo.isEmpty ? null : motivo);
      ref.invalidate(misCitasProvider);
      messenger.showSnackBar(
        const SnackBar(content: Text('Cita cancelada.')),
      );
    } on DioException catch (e) {
      messenger.showSnackBar(
        SnackBar(content: Text(e.message ?? 'No se pudo cancelar la cita.')),
      );
    } finally {
      if (mounted) setState(() => _cancelando = false);
    }
  }

  CitaModel get cita => widget.cita;

  /// Se recalcula en cada build según el ancho disponible: true si la
  /// descripción no entra en 2 líneas (solo entonces tiene sentido desplegar).
  bool _seCorta = false;

  bool get _tieneDescripcion =>
      cita.observaciones != null && cita.observaciones!.trim().isNotEmpty;

  void _alternar() {
    if (_seCorta || _expandida) setState(() => _expandida = !_expandida);
  }

  Widget _descripcion(BuildContext context, double ancho) {
    final theme = Theme.of(context);
    final texto = cita.observaciones!.trim();
    final estilo = theme.textTheme.bodySmall;

    final medidor = TextPainter(
      text: TextSpan(text: texto, style: estilo),
      maxLines: 2,
      textDirection: Directionality.of(context),
      textScaler: MediaQuery.textScalerOf(context),
    )..layout(maxWidth: ancho);
    _seCorta = medidor.didExceedMaxLines;
    medidor.dispose();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          texto,
          maxLines: _expandida ? null : 2,
          overflow: _expandida ? TextOverflow.visible : TextOverflow.ellipsis,
          style: estilo,
        ),
        if (_seCorta) ...[
          const SizedBox(height: 4),
          Row(
            children: [
              Icon(
                _expandida ? Icons.expand_less : Icons.expand_more,
                size: 18,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(width: 2),
              Text(
                _expandida ? 'Ver menos' : 'Ver descripción completa',
                style: theme.textTheme.labelSmall?.copyWith(
                  color: theme.colorScheme.primary,
                ),
              ),
            ],
          ),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final estadoColor =
        _parseHexColor(cita.estadoColor) ??
        Theme.of(context).colorScheme.outline;
    final fecha = DateFormat('dd/MM/yyyy HH:mm').format(cita.fechaHora);

    return Card(
      child: InkWell(
        onTap: _tieneDescripcion ? _alternar : null,
        child: AnimatedSize(
          duration: const Duration(milliseconds: 200),
          curve: Curves.easeOut,
          alignment: Alignment.topCenter,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        cita.vehiculoNombre ?? 'Consulta general',
                        style: Theme.of(context).textTheme.titleSmall
                            ?.copyWith(fontWeight: FontWeight.bold),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(
                            Icons.calendar_today_outlined,
                            size: 14,
                            color: Theme.of(context).colorScheme.outline,
                          ),
                          const SizedBox(width: 6),
                          Text(
                            fecha,
                            style: Theme.of(context).textTheme.bodySmall
                                ?.copyWith(
                                  color: Theme.of(context).colorScheme.outline,
                                ),
                          ),
                        ],
                      ),
                      if (_tieneDescripcion) ...[
                        const SizedBox(height: 6),
                        LayoutBuilder(
                          builder: (context, constraints) =>
                              _descripcion(context, constraints.maxWidth),
                        ),
                      ],
                      if (cita.puedeCancelarse) ...[
                        const SizedBox(height: 8),
                        OutlinedButton.icon(
                          onPressed: _cancelando ? null : _cancelar,
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Theme.of(context).colorScheme.error,
                            side: BorderSide(
                              color: Theme.of(context)
                                  .colorScheme
                                  .error
                                  .withValues(alpha: 0.5),
                            ),
                            visualDensity: VisualDensity.compact,
                          ),
                          icon: _cancelando
                              ? const SizedBox(
                                  width: 16,
                                  height: 16,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(Icons.event_busy_outlined, size: 18),
                          label: const Text('Cancelar cita'),
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 5,
                  ),
                  decoration: BoxDecoration(
                    color: estadoColor.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 8,
                        height: 8,
                        decoration: BoxDecoration(
                          color: estadoColor,
                          shape: BoxShape.circle,
                        ),
                      ),
                      const SizedBox(width: 6),
                      Text(
                        cita.estadoDescripcion,
                        style: TextStyle(
                          color: estadoColor,
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Color? _parseHexColor(String? hex) {
    if (hex == null || hex.isEmpty) return null;
    final cleaned = hex.replaceFirst('#', '');
    final value = int.tryParse(cleaned, radix: 16);
    if (value == null) return null;
    return Color(0xFF000000 | value);
  }
}

/// Confirmación de cancelación con motivo opcional. Devuelve el motivo
/// (cadena vacía si no escribió nada) o null si el usuario no confirma.
class _DialogoCancelar extends StatefulWidget {
  const _DialogoCancelar({required this.cita});

  final CitaModel cita;

  @override
  State<_DialogoCancelar> createState() => _DialogoCancelarState();
}

class _DialogoCancelarState extends State<_DialogoCancelar> {
  final _motivo = TextEditingController();

  @override
  void dispose() {
    _motivo.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final cuando = DateFormat("dd/MM/yyyy 'a las' HH:mm").format(
      widget.cita.fechaHora,
    );

    return AlertDialog(
      title: const Text('¿Cancelar la cita?'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Tu cita del $cuando quedará cancelada. '
                'Si querés, podés agendar otra después.'),
            const SizedBox(height: 16),
            TextField(
              controller: _motivo,
              maxLength: 300,
              maxLines: 3,
              minLines: 1,
              textCapitalization: TextCapitalization.sentences,
              decoration: const InputDecoration(
                labelText: 'Motivo (opcional)',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Volver'),
        ),
        FilledButton(
          style: FilledButton.styleFrom(
            backgroundColor: Theme.of(context).colorScheme.error,
            foregroundColor: Theme.of(context).colorScheme.onError,
            padding: const EdgeInsets.symmetric(horizontal: 20),
          ),
          onPressed: () => Navigator.of(context).pop(_motivo.text.trim()),
          child: const Text('Sí, cancelar'),
        ),
      ],
    );
  }
}
