import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/config/api_config.dart';
import '../../data/models/foto_vehiculo_model.dart';

/// Abre las fotos del vehículo a pantalla completa, empezando por [inicial].
Future<void> abrirVisorFotos(
  BuildContext context,
  List<FotoVehiculoModel> fotos,
  int inicial,
) {
  return Navigator.of(context, rootNavigator: true).push(
    PageRouteBuilder<void>(
      opaque: false,
      barrierColor: Colors.black,
      transitionDuration: const Duration(milliseconds: 200),
      pageBuilder: (_, _, _) => VisorFotos(fotos: fotos, inicial: inicial),
      transitionsBuilder: (_, animacion, _, child) =>
          FadeTransition(opacity: animacion, child: child),
    ),
  );
}

/// Visor a pantalla completa: se desliza entre fotos, se pellizca para hacer
/// zoom (hasta 4x) y doble toque acerca o vuelve al tamaño original. Mientras
/// una foto está ampliada, el deslizamiento mueve la foto en vez de pasar a la
/// siguiente.
class VisorFotos extends StatefulWidget {
  const VisorFotos({super.key, required this.fotos, required this.inicial});

  final List<FotoVehiculoModel> fotos;
  final int inicial;

  @override
  State<VisorFotos> createState() => _VisorFotosState();
}

class _VisorFotosState extends State<VisorFotos> {
  late final PageController _paginas = PageController(
    initialPage: widget.inicial,
  );
  late int _actual = widget.inicial;
  bool _ampliada = false;

  @override
  void dispose() {
    _paginas.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final total = widget.fotos.length;

    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light,
      child: Scaffold(
        backgroundColor: Colors.black,
        body: Stack(
          children: [
            PageView.builder(
              controller: _paginas,
              itemCount: total,
              physics: _ampliada
                  ? const NeverScrollableScrollPhysics()
                  : const PageScrollPhysics(),
              onPageChanged: (i) => setState(() {
                _actual = i;
                _ampliada = false;
              }),
              itemBuilder: (_, i) => _FotoAmpliable(
                url: '${ApiConfig.mediaBaseUrl}${widget.fotos[i].url}',
                onZoomCambiado: (ampliada) {
                  if (ampliada != _ampliada) {
                    setState(() => _ampliada = ampliada);
                  }
                },
              ),
            ),
            SafeArea(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                child: Row(
                  children: [
                    IconButton(
                      tooltip: 'Cerrar',
                      onPressed: () => Navigator.of(context).pop(),
                      icon: const Icon(Icons.close, color: Colors.white),
                      style: IconButton.styleFrom(
                        backgroundColor: Colors.black45,
                      ),
                    ),
                    const Spacer(),
                    if (total > 1)
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 6,
                        ),
                        decoration: BoxDecoration(
                          color: Colors.black45,
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Text(
                          '${_actual + 1} / $total',
                          style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
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

class _FotoAmpliable extends StatefulWidget {
  const _FotoAmpliable({required this.url, required this.onZoomCambiado});

  final String url;
  final ValueChanged<bool> onZoomCambiado;

  @override
  State<_FotoAmpliable> createState() => _FotoAmpliableState();
}

class _FotoAmpliableState extends State<_FotoAmpliable>
    with SingleTickerProviderStateMixin {
  static const double _zoomDobleToque = 2.5;

  final TransformationController _transformacion = TransformationController();
  late final AnimationController _animacion = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 220),
  )..addListener(() => _transformacion.value = _tween!.evaluate(_animacion));
  Matrix4Tween? _tween;
  TapDownDetails? _ultimoToque;

  @override
  void dispose() {
    _animacion.dispose();
    _transformacion.dispose();
    super.dispose();
  }

  bool get _estaAmpliada => _transformacion.value.getMaxScaleOnAxis() > 1.01;

  void _avisarZoom() => widget.onZoomCambiado(_estaAmpliada);

  /// Doble toque: si está al tamaño original, acerca 2,5x sobre el punto
  /// tocado; si ya está ampliada, vuelve al tamaño original.
  void _dobleToque() {
    final destino = Matrix4.identity();
    if (!_estaAmpliada && _ultimoToque != null) {
      final p = _ultimoToque!.localPosition;
      destino
        ..translateByDouble(
          -p.dx * (_zoomDobleToque - 1),
          -p.dy * (_zoomDobleToque - 1),
          0,
          1,
        )
        ..scaleByDouble(_zoomDobleToque, _zoomDobleToque, 1, 1);
    }
    _tween = Matrix4Tween(begin: _transformacion.value, end: destino);
    _animacion.forward(from: 0).whenComplete(_avisarZoom);
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onDoubleTapDown: (d) => _ultimoToque = d,
      onDoubleTap: _dobleToque,
      child: InteractiveViewer(
        transformationController: _transformacion,
        minScale: 1,
        maxScale: 4,
        onInteractionEnd: (_) => _avisarZoom(),
        child: SizedBox.expand(
          child: CachedNetworkImage(
            imageUrl: widget.url,
            fit: BoxFit.contain,
            placeholder: (_, _) => const Center(
              child: CircularProgressIndicator(
                color: Colors.white,
                strokeWidth: 2,
              ),
            ),
            errorWidget: (_, _, _) => const Center(
              child: Icon(
                Icons.broken_image_outlined,
                color: Colors.white54,
                size: 48,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
