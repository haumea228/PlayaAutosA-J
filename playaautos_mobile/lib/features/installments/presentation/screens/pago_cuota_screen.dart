import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_web_auth_2/flutter_web_auth_2.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/services/camera_service.dart';
import '../../../../shared/utils/formatters.dart';
import '../../../documents/presentation/providers/documentos_provider.dart';
import '../../data/models/cuota_model.dart';
import '../../data/models/pago_online_model.dart';
import '../providers/cuotas_provider.dart';

enum _MetodoPago { tarjeta, transferencia }

/// Pago de una cuota. Conviven dos formas:
/// - Tarjeta: página de pago de Stripe (entorno de pruebas). Se confirma sola:
///   al volver a la app, la API verifica el cobro contra Stripe y registra el
///   pago (cuota pagada, ticket disponible).
/// - Transferencia: el cliente adjunta la foto del comprobante y la cuota
///   queda "En revisión" hasta que el encargado la apruebe desde la web.
class PagoCuotaScreen extends ConsumerStatefulWidget {
  const PagoCuotaScreen({super.key, required this.cuotaId});

  final String cuotaId;

  @override
  ConsumerState<PagoCuotaScreen> createState() => _PagoCuotaScreenState();
}

class _PagoCuotaScreenState extends ConsumerState<PagoCuotaScreen> {
  final CameraService _camera = CameraService();
  final TextEditingController _observacion = TextEditingController();
  File? _comprobante;

  _MetodoPago? _metodo;
  PagoOnlineModel? _pagoOnline;
  bool _procesandoTarjeta = false;
  bool _terminado = false;

  @override
  void dispose() {
    _observacion.dispose();
    super.dispose();
  }

  // ── Tarjeta ─────────────────────────────────────────────────────────────

  Future<void> _pagarConTarjeta(CuotaModel cuota) async {
    setState(() => _procesandoTarjeta = true);
    PagoOnlineModel? pago;
    try {
      pago = await ref
          .read(installmentsRepositoryProvider)
          .iniciarPagoConTarjeta(cuota.id);
    } on DioException catch (e) {
      _snack(e.message ?? 'No se pudo iniciar el pago con tarjeta.');
    } finally {
      if (mounted) setState(() => _procesandoTarjeta = false);
    }
    if (pago == null || !mounted) return;

    setState(() => _pagoOnline = pago);
    await _abrirPaginaDePago(pago);
  }

  /// Abre la página de pago de Stripe en una pestaña del navegador. Al pagar
  /// (o cancelar), Stripe redirige a playaautos://pago/..., la pestaña se
  /// cierra sola y se verifica el cobro contra la API.
  Future<void> _abrirPaginaDePago(PagoOnlineModel pago) async {
    final url = pago.url;
    if (url == null) return;
    try {
      final retorno = await FlutterWebAuth2.authenticate(
        url: url,
        callbackUrlScheme: 'playaautos',
      );
      final cancelado = Uri.parse(retorno).path.contains('cancelado');
      if (cancelado) _snack('Cancelaste el pago. No se realizó ningún cobro.');
      await _verificarPagoConTarjeta(silencioso: cancelado);
    } on PlatformException {
      // Cerró la pestaña sin terminar: igual se consulta, por si ya había pagado.
      await _verificarPagoConTarjeta(silencioso: true);
    }
  }

  Future<void> _verificarPagoConTarjeta({bool silencioso = false}) async {
    final pago = _pagoOnline;
    if (pago == null || _procesandoTarjeta || _terminado) return;

    setState(() => _procesandoTarjeta = true);
    try {
      final actualizado = await ref
          .read(installmentsRepositoryProvider)
          .verificarPagoConTarjeta(pago.id);
      if (!mounted) return;

      if (actualizado.estaPagado) {
        _terminado = true;
        ref.invalidate(misCuotasProvider);
        ref.invalidate(misTicketsCuotaProvider);
        _snack('¡Pago confirmado! Tu cuota ya figura como pagada.');
        _salir();
        return;
      }
      if (actualizado.requiereReembolso) {
        setState(() => _pagoOnline = null);
        ref.invalidate(misCuotasProvider);
        await _avisar(
          'Pago duplicado',
          'Recibimos tu pago con tarjeta, pero la cuota ya había sido pagada por '
              'otro medio. Comunicate con la playa para gestionar el reembolso.',
        );
        return;
      }
      if (!actualizado.estaPendiente) {
        setState(() => _pagoOnline = null);
        _snack('La página de pago venció. Podés generar una nueva.');
        return;
      }
      setState(() => _pagoOnline = actualizado);
      if (!silencioso) {
        _snack('Todavía no recibimos la confirmación del pago.');
      }
    } on DioException catch (e) {
      if (!silencioso) _snack(e.message ?? 'No se pudo verificar el pago.');
    } finally {
      if (mounted) setState(() => _procesandoTarjeta = false);
    }
  }

  // ── Transferencia ───────────────────────────────────────────────────────

  Future<void> _elegirImagen({required bool desdeCamara}) async {
    try {
      final file = desdeCamara
          ? await _camera.pickImageFromCamera()
          : await _camera.pickImageFromGallery();
      if (file == null) {
        _snack(desdeCamara
            ? 'No se tomó ninguna foto (o falta el permiso de cámara).'
            : 'No se eligió ninguna imagen (o falta el permiso de galería).');
        return;
      }
      setState(() => _comprobante = file);
    } catch (_) {
      _snack('No se pudo obtener la imagen.');
    }
  }

  Future<void> _enviarComprobante(CuotaModel cuota) async {
    final archivo = _comprobante;
    if (archivo == null) return;

    final confirmar = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Enviar comprobante'),
        content: Text(
          '¿Confirmás que transferiste ${Formatters.guaranies(cuota.montoAPagar)} '
          'para la cuota N° ${cuota.numeroCuota}?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Enviar'),
          ),
        ],
      ),
    );
    if (confirmar != true) return;

    await ref.read(reportarPagoProvider.notifier).enviar(
          cuotaId: cuota.id,
          comprobante: archivo,
          observacion: _observacion.text,
        );
  }

  // ── Utilidades ──────────────────────────────────────────────────────────

  void _snack(String mensaje) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(mensaje)));
  }

  Future<void> _avisar(String titulo, String texto) {
    return showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(titulo),
        content: Text(texto),
        actions: [
          FilledButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Entendido'),
          ),
        ],
      ),
    );
  }

  void _salir() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go('/mis-cuotas');
    }
  }

  @override
  Widget build(BuildContext context) {
    final id = int.tryParse(widget.cuotaId);
    final cuotasAsync = ref.watch(misCuotasProvider);
    final envio = ref.watch(reportarPagoProvider);

    ref.listen<ReportarPagoState>(reportarPagoProvider, (anterior, actual) {
      if (actual.mensaje != null && anterior?.mensaje == null) {
        _terminado = true;
        _snack(actual.mensaje!);
        _salir();
      } else if (actual.error != null && actual.error != anterior?.error) {
        _snack(actual.error!);
      }
    });

    return Scaffold(
      appBar: AppBar(title: const Text('Pagar cuota')),
      body: cuotasAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stack) => _Mensaje(
          icono: Icons.error_outline,
          texto: 'No se pudo cargar la cuota.\n$error',
          accion: FilledButton(
            onPressed: () => ref.invalidate(misCuotasProvider),
            child: const Text('Reintentar'),
          ),
        ),
        data: (cuotas) {
          final cuota = cuotas.where((c) => c.id == id).firstOrNull;
          if (cuota == null) {
            return const _Mensaje(
              icono: Icons.search_off,
              texto: 'No encontramos esta cuota.',
            );
          }
          if (!cuota.puedeInformarPago && !_terminado) {
            return _Mensaje(
              icono: cuota.estaPagada ? Icons.check_circle_outline : Icons.info_outline,
              texto: cuota.estaPagada
                  ? 'Esta cuota ya está pagada.'
                  : cuota.motivoNoPuedePagar ?? 'Esta cuota no admite pagos por ahora.',
            );
          }
          final metodo = _metodo ??
              (cuota.pagoConTarjetaDisponible ? _MetodoPago.tarjeta : _MetodoPago.transferencia);
          return _formulario(context, cuota, metodo, envio.isLoading);
        },
      ),
    );
  }

  Widget _formulario(BuildContext context, CuotaModel cuota, _MetodoPago metodo, bool enviando) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final total = cuota.cantidadCuotas;
    final ocupado = enviando || _procesandoTarjeta;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          margin: EdgeInsets.zero,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(cuota.vehiculo, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 2),
                Text(
                  '${total != null ? 'Cuota ${cuota.numeroCuota} de $total' : 'Cuota N° ${cuota.numeroCuota}'}'
                  ' · vence el ${Formatters.fecha(cuota.fechaVencimiento)}',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: cuota.estaVencida ? colors.error : colors.outline,
                  ),
                ),
                const Divider(height: 24),
                _FilaMonto(etiqueta: 'Cuota', valor: Formatters.guaranies(cuota.monto)),
                if (cuota.recargoCalculado > 0)
                  _FilaMonto(
                    etiqueta: 'Recargo por mora',
                    valor: Formatters.guaranies(cuota.recargoCalculado),
                    color: colors.error,
                  ),
                const SizedBox(height: 6),
                _FilaMonto(
                  etiqueta: 'Total a pagar',
                  valor: Formatters.guaranies(cuota.montoAPagar),
                  destacado: true,
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        if (cuota.pagoConTarjetaDisponible) ...[
          Text('¿Cómo querés pagar?', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          SegmentedButton<_MetodoPago>(
            segments: const [
              ButtonSegment(
                value: _MetodoPago.tarjeta,
                icon: Icon(Icons.credit_card),
                label: Text('Tarjeta'),
              ),
              ButtonSegment(
                value: _MetodoPago.transferencia,
                icon: Icon(Icons.account_balance),
                label: Text('Transferencia'),
              ),
            ],
            selected: {metodo},
            onSelectionChanged: ocupado || _pagoOnline != null
                ? null
                : (s) => setState(() => _metodo = s.first),
          ),
          const SizedBox(height: 16),
        ],
        if (metodo == _MetodoPago.tarjeta)
          ..._seccionTarjeta(context, cuota, ocupado)
        else
          ..._seccionTransferencia(context, cuota, enviando),
      ],
    );
  }

  List<Widget> _seccionTarjeta(BuildContext context, CuotaModel cuota, bool ocupado) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final pendiente = _pagoOnline?.estaPendiente ?? false;

    return [
      _Info(
        icono: Icons.lock_outline,
        texto: 'Vas a pagar en la página segura de Stripe. El pago se confirma al '
            'instante: tu cuota queda pagada y el ticket disponible, sin esperar revisión.',
      ),
      const SizedBox(height: 10),
      Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: const Color(0xFFCA8A04).withValues(alpha: 0.10),
          borderRadius: BorderRadius.circular(16),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Icon(Icons.science_outlined, color: Color(0xFFCA8A04), size: 20),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                'Entorno de pruebas: no se cobra dinero real. Usá la tarjeta '
                '4242 4242 4242 4242, cualquier fecha futura y cualquier CVC.',
                style: theme.textTheme.bodySmall,
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: 16),
      if (pendiente) ...[
        Card(
          margin: EdgeInsets.zero,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(Icons.hourglass_top, color: colors.primary),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text('Esperando la confirmación del pago',
                          style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Text(
                  'Si ya pagaste, tocá "Ya pagué" para confirmarlo. Si cerraste la '
                  'página antes de terminar, podés volver a abrirla.',
                  style: theme.textTheme.bodySmall,
                ),
                const SizedBox(height: 12),
                FilledButton.icon(
                  onPressed: ocupado ? null : () => _verificarPagoConTarjeta(),
                  icon: ocupado
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.check),
                  label: const Text('Ya pagué, verificar'),
                ),
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  onPressed: ocupado ? null : () => _abrirPaginaDePago(_pagoOnline!),
                  icon: const Icon(Icons.open_in_new),
                  label: const Text('Abrir la página de pago'),
                ),
              ],
            ),
          ),
        ),
      ] else
        FilledButton.icon(
          onPressed: ocupado ? null : () => _pagarConTarjeta(cuota),
          icon: ocupado
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                )
              : const Icon(Icons.credit_card),
          label: Text('Pagar ${Formatters.guaranies(cuota.montoAPagar)} con tarjeta'),
        ),
    ];
  }

  List<Widget> _seccionTransferencia(BuildContext context, CuotaModel cuota, bool enviando) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return [
      const _Info(
        icono: Icons.info_outline,
        texto: 'Adjuntá una foto clara del comprobante de transferencia. '
            'El encargado va a verificar el pago y, cuando lo apruebe, '
            'tu cuota figurará como pagada y podrás descargar el ticket.',
      ),
      const SizedBox(height: 16),
      Text('Comprobante', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
      const SizedBox(height: 8),
      if (_comprobante != null)
        Stack(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: Image.file(
                _comprobante!,
                height: 320,
                width: double.infinity,
                fit: BoxFit.contain,
              ),
            ),
            Positioned(
              top: 8,
              right: 8,
              child: IconButton.filledTonal(
                tooltip: 'Quitar imagen',
                onPressed: enviando ? null : () => setState(() => _comprobante = null),
                icon: const Icon(Icons.close),
              ),
            ),
          ],
        )
      else
        Container(
          height: 140,
          decoration: BoxDecoration(
            border: Border.all(color: colors.outline.withValues(alpha: 0.5)),
            borderRadius: BorderRadius.circular(16),
          ),
          alignment: Alignment.center,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.receipt_long_outlined, size: 40, color: colors.outline),
              const SizedBox(height: 6),
              Text('Todavía no adjuntaste el comprobante',
                  style: theme.textTheme.bodySmall?.copyWith(color: colors.outline)),
            ],
          ),
        ),
      const SizedBox(height: 10),
      Row(
        children: [
          Expanded(
            child: OutlinedButton.icon(
              onPressed: enviando ? null : () => _elegirImagen(desdeCamara: true),
              icon: const Icon(Icons.photo_camera_outlined),
              label: const Text('Cámara'),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: OutlinedButton.icon(
              onPressed: enviando ? null : () => _elegirImagen(desdeCamara: false),
              icon: const Icon(Icons.photo_library_outlined),
              label: const Text('Galería'),
            ),
          ),
        ],
      ),
      const SizedBox(height: 16),
      TextField(
        controller: _observacion,
        enabled: !enviando,
        maxLength: 500,
        maxLines: 2,
        decoration: const InputDecoration(
          labelText: 'Comentario (opcional)',
          hintText: 'Ej.: transferí desde la cuenta de mi esposa',
        ),
      ),
      const SizedBox(height: 8),
      FilledButton.icon(
        onPressed: _comprobante == null || enviando ? null : () => _enviarComprobante(cuota),
        icon: enviando
            ? const SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
              )
            : const Icon(Icons.send),
        label: Text(enviando ? 'Enviando...' : 'Enviar comprobante'),
      ),
    ];
  }
}

class _Info extends StatelessWidget {
  const _Info({required this.icono, required this.texto});

  final IconData icono;
  final String texto;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: colors.primary.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icono, color: colors.primary, size: 20),
          const SizedBox(width: 10),
          Expanded(child: Text(texto, style: Theme.of(context).textTheme.bodySmall)),
        ],
      ),
    );
  }
}

class _FilaMonto extends StatelessWidget {
  const _FilaMonto({
    required this.etiqueta,
    required this.valor,
    this.color,
    this.destacado = false,
  });

  final String etiqueta;
  final String valor;
  final Color? color;
  final bool destacado;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final estilo = destacado
        ? theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.bold,
            color: theme.colorScheme.primary,
          )
        : theme.textTheme.bodyMedium?.copyWith(color: color);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Expanded(child: Text(etiqueta, style: destacado ? theme.textTheme.titleSmall : estilo)),
          Text(valor, style: estilo),
        ],
      ),
    );
  }
}

class _Mensaje extends StatelessWidget {
  const _Mensaje({required this.icono, required this.texto, this.accion});

  final IconData icono;
  final String texto;
  final Widget? accion;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icono, size: 44, color: Theme.of(context).colorScheme.outline),
            const SizedBox(height: 12),
            Text(texto, textAlign: TextAlign.center),
            if (accion != null) ...[const SizedBox(height: 12), accion!],
          ],
        ),
      ),
    );
  }
}
