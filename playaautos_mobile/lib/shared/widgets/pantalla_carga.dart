import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../config/theme/app_theme.dart';
import '../../features/catalog/presentation/providers/catalogo_provider.dart';

/// Pantalla de carga al abrir la app: continúa la pantalla de inicio nativa
/// de Android (fondo blanco + logo, ver res/values-v31/styles.xml) con el
/// logo en el mismo lugar y un indicador girando debajo.
///
/// Es una capa encima de la app, no una ruta: por debajo la app ya arrancó
/// (el catálogo se está cargando, y si se abrió tocando una notificación ya
/// navegó a citas/cuotas), así que no interfiere con la navegación. Se
/// desvanece cuando el catálogo terminó de cargar (o falló), con un mínimo
/// para que no parpadee y un máximo para no dejar al usuario esperando.
class PantallaCarga extends ConsumerStatefulWidget {
  const PantallaCarga({super.key, required this.child});

  final Widget child;

  /// Tamaño del ícono adaptativo en la pantalla nativa de Android 12+
  /// (medido: el logo, que ocupa el 62 % de la imagen, mide ~121 dp).
  static const double tamanoLogo = 196;

  static const Duration _minimo = Duration(milliseconds: 1200);
  static const Duration _maximo = Duration(seconds: 8);

  @override
  ConsumerState<PantallaCarga> createState() => _PantallaCargaState();
}

class _PantallaCargaState extends ConsumerState<PantallaCarga>
    with SingleTickerProviderStateMixin {
  late final AnimationController _salida = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 350),
  );
  bool _visible = true;
  bool _logoPedido = false;
  final Completer<void> _primerCuadro = Completer<void>();

  static const _logo = AssetImage('assets/icon/icono_adaptativo.png');

  @override
  void initState() {
    super.initState();
    // Android mantiene su pantalla nativa hasta el primer cuadro de Flutter:
    // se retrasa ese cuadro hasta tener el logo decodificado, así no se ve
    // un instante en blanco entre una pantalla y la otra.
    WidgetsBinding.instance.deferFirstFrame();
    _esperar();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_logoPedido) return;
    _logoPedido = true;
    precacheImage(_logo, context)
        .timeout(const Duration(seconds: 2), onTimeout: () {})
        .whenComplete(() {
          WidgetsBinding.instance.allowFirstFrame();
          WidgetsBinding.instance.endOfFrame.then((_) => _primerCuadro.complete());
        });
  }

  Future<void> _esperar() async {
    // Precarga del catálogo: es la primera pantalla, así al desaparecer la
    // capa ya se ven los vehículos. Si la API no responde, se sigue igual
    // (el catálogo muestra su propio error con "Reintentar").
    final catalogo = ref
        .read(catalogoProvider.future)
        .then<void>((_) {}, onError: (_) {})
        .timeout(PantallaCarga._maximo, onTimeout: () {});

    // El mínimo cuenta desde que la pantalla de carga ya se ve (el primer
    // cuadro se retrasa hasta tener el logo), no desde que arrancó la app.
    await _primerCuadro.future;
    await Future.wait([Future<void>.delayed(PantallaCarga._minimo), catalogo]);
    if (!mounted) return;
    await _salida.forward();
    if (mounted) setState(() => _visible = false);
  }

  @override
  void dispose() {
    _salida.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        widget.child,
        if (_visible)
          FadeTransition(
            opacity: ReverseAnimation(_salida),
            child: AnnotatedRegion<SystemUiOverlayStyle>(
              value: SystemUiOverlayStyle.dark,
              child: ColoredBox(
                color: Colors.white,
                child: Stack(
                  children: [
                    Center(
                      child: Image(
                        image: _logo,
                        width: PantallaCarga.tamanoLogo,
                        height: PantallaCarga.tamanoLogo,
                        gaplessPlayback: true,
                      ),
                    ),
                    // Debajo del logo (el centro del indicador queda 115 dp
                    // por debajo del centro de la pantalla).
                    const Center(
                      child: Padding(
                        padding: EdgeInsets.only(top: 230),
                        child: SizedBox(
                          width: 32,
                          height: 32,
                          child: CircularProgressIndicator(
                            strokeWidth: 3,
                            color: AppColors.tealPetroleo,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
      ],
    );
  }
}
