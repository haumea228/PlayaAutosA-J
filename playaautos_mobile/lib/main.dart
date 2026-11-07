import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'config/router/app_router.dart';
import 'config/theme/app_theme.dart';
import 'core/network/dio_client.dart';
import 'core/services/notification_service.dart';
import 'features/appointments/presentation/providers/citas_provider.dart';
import 'features/documents/presentation/providers/documentos_provider.dart';
import 'features/installments/presentation/providers/cuotas_provider.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await NotificationService.instance.inicializar();
  runApp(const ProviderScope(child: PlayaAutosApp()));

  // Sesión ya iniciada de una ejecución anterior: mantener el token de push
  // actualizado en la API (si venció el JWT, la API responde 401 y no pasa nada).
  if (await DioClient.getToken() != null) {
    unawaited(NotificationService.instance.registrarDispositivo());
  }
}

class PlayaAutosApp extends ConsumerStatefulWidget {
  const PlayaAutosApp({super.key});

  @override
  ConsumerState<PlayaAutosApp> createState() => _PlayaAutosAppState();
}

class _PlayaAutosAppState extends ConsumerState<PlayaAutosApp> {
  StreamSubscription<NotificacionEvento>? _suscripcion;

  @override
  void initState() {
    super.initState();
    // Un aviso de la API significa que cambió algo en el servidor: se
    // refrescan los datos cacheados para que la pantalla muestre el estado
    // nuevo (cita confirmada, cuota pagada, etc.).
    _suscripcion = NotificationService.instance.eventos.listen((evento) {
      switch (evento.tipo) {
        case 'cita':
          ref.invalidate(misCitasProvider);
        case 'cuota':
          ref.invalidate(misCuotasProvider);
          ref.invalidate(misTicketsCuotaProvider);
      }
    });
  }

  @override
  void dispose() {
    _suscripcion?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'PlayaAutos',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      routerConfig: AppRouter.router,
    );
  }
}
