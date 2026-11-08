import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'config/router/app_router.dart';
import 'config/theme/app_theme.dart';
import 'core/network/dio_client.dart';
import 'core/services/notification_service.dart';
import 'features/appointments/presentation/providers/citas_provider.dart';
import 'features/documents/presentation/providers/documentos_provider.dart';
import 'features/installments/presentation/providers/cuotas_provider.dart';
import 'shared/widgets/pantalla_carga.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Primero se muestra la app (con la pantalla de carga) y después se
  // inicializa Firebase: así esa espera se ve con el indicador girando en
  // lugar de alargar la pantalla de inicio nativa, que es estática.
  runApp(const ProviderScope(child: PlayaAutosApp()));
  await NotificationService.instance.inicializar();

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
      // Calendarios, diálogos y textos de los componentes de Flutter en
      // español (sin esto, showDatePicker mostraba "Select date", "Cancel"...).
      locale: const Locale('es'),
      supportedLocales: const [Locale('es'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      routerConfig: AppRouter.router,
      builder: (context, child) => PantallaCarga(child: child!),
    );
  }
}
