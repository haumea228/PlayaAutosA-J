import 'package:cached_network_image/cached_network_image.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/config/api_config.dart';
import '../../../../shared/utils/formatters.dart';
import '../../data/asistente_repository.dart';
import '../../data/models/mensaje_asistente.dart';

/// Abre el asistente como panel inferior. Se abre sobre el navegador raíz para
/// que, al tocar un vehículo recomendado, el detalle se apile encima y al
/// volver la conversación siga ahí.
Future<void> mostrarAsistente(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    useRootNavigator: true,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => const AsistenteSheet(),
  );
}

class AsistenteSheet extends StatefulWidget {
  const AsistenteSheet({super.key});

  @override
  State<AsistenteSheet> createState() => _AsistenteSheetState();
}

class _AsistenteSheetState extends State<AsistenteSheet> {
  static const List<String> _sugerencias = [
    'Toyotas con menos de 120 mil km',
    'Automático de menos de 40 millones',
    'Algo familiar y económico',
    'Una camioneta 4x4 diésel',
  ];

  final AsistenteRepository _repositorio = AsistenteRepository();
  final TextEditingController _texto = TextEditingController();
  final ScrollController _scroll = ScrollController();
  final List<MensajeAsistente> _mensajes = [];
  bool _cargando = false;

  @override
  void dispose() {
    _texto.dispose();
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _enviar([String? sugerencia]) async {
    final pregunta = (sugerencia ?? _texto.text).trim();
    if (pregunta.isEmpty || _cargando) return;

    final previos = List<MensajeAsistente>.of(_mensajes);
    setState(() {
      _mensajes.add(MensajeAsistente(esUsuario: true, texto: pregunta));
      _cargando = true;
      _texto.clear();
    });
    _bajar();

    MensajeAsistente respuesta;
    try {
      respuesta = await _repositorio.consultar(pregunta, previos);
    } on DioException catch (e) {
      respuesta = MensajeAsistente(
        esUsuario: false,
        texto: e.message ?? 'No pude responder ahora. Probá de nuevo en un momento.',
        esError: true,
      );
    }

    if (!mounted) return;
    setState(() {
      _mensajes.add(respuesta);
      _cargando = false;
    });
    _bajar();
  }

  void _bajar() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent,
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final pantalla = MediaQuery.sizeOf(context).height;
    final teclado = MediaQuery.viewInsetsOf(context).bottom;
    // 85% de la pantalla, pero sin pasarse del espacio que deja el teclado
    // (descontando la barra de estado y la manija del panel).
    final disponible = pantalla - teclado - MediaQuery.paddingOf(context).top - 48;
    final alto = (pantalla * 0.85).clamp(0.0, disponible);

    return Padding(
      padding: EdgeInsets.only(bottom: teclado),
      child: SizedBox(
        height: alto,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 12, 8),
              child: Row(
                children: [
                  CircleAvatar(
                    backgroundColor: colors.primary,
                    child: const Icon(Icons.auto_awesome, color: Colors.white, size: 20),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Asistente A&J',
                            style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                        Text('Te recomiendo vehículos del catálogo',
                            style: theme.textTheme.bodySmall?.copyWith(color: colors.outline)),
                      ],
                    ),
                  ),
                  IconButton(
                    tooltip: 'Cerrar',
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.close),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView(
                controller: _scroll,
                padding: const EdgeInsets.all(16),
                children: [
                  _Burbuja(
                    mensaje: const MensajeAsistente(
                      esUsuario: false,
                      texto: '¡Hola! Contame qué vehículo buscás: marca, presupuesto, '
                          'kilometraje, si lo querés automático o diésel... y te recomiendo '
                          'opciones de nuestro catálogo.',
                    ),
                  ),
                  if (_mensajes.isEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 4, bottom: 8),
                      child: Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          for (final s in _sugerencias)
                            ActionChip(
                              avatar: Icon(Icons.search, size: 16, color: colors.primary),
                              label: Text(s),
                              onPressed: () => _enviar(s),
                            ),
                        ],
                      ),
                    ),
                  for (final m in _mensajes) _Burbuja(mensaje: m),
                  if (_cargando) const _Escribiendo(),
                ],
              ),
            ),
            const Divider(height: 1),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(12, 8, 8, 8),
                child: Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _texto,
                        enabled: !_cargando,
                        maxLength: 500,
                        minLines: 1,
                        maxLines: 3,
                        textInputAction: TextInputAction.send,
                        onSubmitted: (_) => _enviar(),
                        decoration: const InputDecoration(
                          hintText: 'Escribí lo que buscás...',
                          counterText: '',
                          contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    IconButton.filled(
                      tooltip: 'Enviar',
                      onPressed: _cargando ? null : _enviar,
                      icon: const Icon(Icons.send),
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

class _Burbuja extends StatelessWidget {
  const _Burbuja({required this.mensaje});

  final MensajeAsistente mensaje;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final usuario = mensaje.esUsuario;

    final fondo = usuario
        ? colors.primary
        : mensaje.esError
            ? colors.errorContainer
            : colors.secondaryContainer.withValues(alpha: 0.6);
    final texto = usuario
        ? Colors.white
        : mensaje.esError
            ? colors.onErrorContainer
            : colors.onSurface;

    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Column(
        crossAxisAlignment: usuario ? CrossAxisAlignment.end : CrossAxisAlignment.start,
        children: [
          ConstrainedBox(
            constraints: BoxConstraints(maxWidth: MediaQuery.sizeOf(context).width * 0.8),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: fondo,
                borderRadius: BorderRadius.only(
                  topLeft: const Radius.circular(18),
                  topRight: const Radius.circular(18),
                  bottomLeft: Radius.circular(usuario ? 18 : 4),
                  bottomRight: Radius.circular(usuario ? 4 : 18),
                ),
              ),
              child: Text(mensaje.texto, style: TextStyle(color: texto)),
            ),
          ),
          for (final v in mensaje.vehiculos) _TarjetaVehiculo(vehiculo: v),
        ],
      ),
    );
  }
}

class _TarjetaVehiculo extends StatelessWidget {
  const _TarjetaVehiculo({required this.vehiculo});

  final VehiculoRecomendado vehiculo;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    final detalles = [
      '${vehiculo.anio}',
      if (vehiculo.kilometraje != null) '${Formatters.numero(vehiculo.kilometraje!)} km',
      if (vehiculo.transmision != null) vehiculo.transmision!,
      if (vehiculo.combustible != null) vehiculo.combustible!,
    ].join(' · ');

    return Card(
      margin: const EdgeInsets.only(top: 8),
      elevation: 1,
      child: InkWell(
        onTap: () => context.push('/catalogo/${vehiculo.id}'),
        child: Row(
          children: [
            SizedBox(
              width: 96,
              height: 76,
              child: vehiculo.fotoPrincipal == null
                  ? Container(
                      color: colors.secondaryContainer,
                      child: Icon(Icons.directions_car, color: colors.onSecondaryContainer),
                    )
                  : CachedNetworkImage(
                      imageUrl: '${ApiConfig.mediaBaseUrl}${vehiculo.fotoPrincipal}',
                      fit: BoxFit.cover,
                      errorWidget: (context, url, error) => Container(
                        color: colors.secondaryContainer,
                        child: Icon(Icons.broken_image_outlined, color: colors.onSecondaryContainer),
                      ),
                    ),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${vehiculo.marca} ${vehiculo.modelo}',
                      style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      detalles,
                      style: theme.textTheme.bodySmall?.copyWith(color: colors.outline),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 2),
                    Text(
                      Formatters.guaranies(vehiculo.precioVenta),
                      style: theme.textTheme.titleSmall?.copyWith(
                        color: colors.primary,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            Icon(Icons.chevron_right, color: colors.outline),
            const SizedBox(width: 4),
          ],
        ),
      ),
    );
  }
}

class _Escribiendo extends StatelessWidget {
  const _Escribiendo();

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: colors.secondaryContainer.withValues(alpha: 0.6),
          borderRadius: BorderRadius.circular(18),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2)),
            const SizedBox(width: 10),
            Text('Buscando opciones...', style: TextStyle(color: colors.outline)),
          ],
        ),
      ),
    );
  }
}
