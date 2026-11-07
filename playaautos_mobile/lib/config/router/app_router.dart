import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/dio_client.dart';
import '../../features/appointments/presentation/screens/agendar_cita_screen.dart';
import '../../features/appointments/presentation/screens/mis_citas_screen.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/auth/presentation/screens/registro_screen.dart';
import '../../features/catalog/presentation/screens/catalogo_screen.dart';
import '../../features/catalog/presentation/screens/detalle_vehiculo_screen.dart';
import '../../features/documents/presentation/screens/documentos_screen.dart';
import '../../features/installments/presentation/screens/mis_cuotas_screen.dart';
import '../../features/installments/presentation/screens/pago_cuota_screen.dart';
import '../../features/profile/presentation/screens/perfil_screen.dart';
import '../../shared/widgets/main_scaffold.dart';

class AppRouter {
  static final GlobalKey<NavigatorState> _rootNavigatorKey =
      GlobalKey<NavigatorState>(debugLabel: 'root');
  static final GlobalKey<NavigatorState> _shellNavigatorKey =
      GlobalKey<NavigatorState>(debugLabel: 'shell');

  // Login/registro son siempre públicos. El catálogo (listado y detalle)
  // también es público según los objetivos del proyecto: cualquiera puede
  // explorar vehículos sin cuenta; solo citas/cuotas/archivos/perfil
  // requieren cliente autenticado.
  static const List<String> _publicRoutes = ['/login', '/registro'];
  static const String _publicPrefix = '/catalogo';

  static final GoRouter router = GoRouter(
    navigatorKey: _rootNavigatorKey,
    initialLocation: '/catalogo',
    redirect: _redirect,
    // Re-evalúa el redirect apenas DioClient detecta un 401 y borra el
    // token (ver sessionNotifier en dio_client.dart), en vez de esperar a
    // que el usuario navegue manualmente: así una sesión vencida manda a
    // /login al toque en lugar de dejar al usuario en una pantalla de error
    // sin salida.
    refreshListenable: DioClient.sessionNotifier,
    routes: [
      GoRoute(
        path: '/login',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/registro',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => const RegistroScreen(),
      ),
      GoRoute(
        path: '/catalogo/:id',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => DetalleVehiculoScreen(
          vehiculoId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        // Sin vehiculoId: "Agendar nueva cita" desde Mis Citas (consulta
        // general, no atada a un auto puntual).
        path: '/agendar-cita',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => const AgendarCitaScreen(),
      ),
      GoRoute(
        // Con vehiculoId: "Agendar cita" desde el detalle de un vehículo.
        path: '/agendar-cita/:vehiculoId',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => AgendarCitaScreen(
          vehiculoId: state.pathParameters['vehiculoId'],
        ),
      ),
      GoRoute(
        path: '/pagar-cuota/:cuotaId',
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => PagoCuotaScreen(
          cuotaId: state.pathParameters['cuotaId']!,
        ),
      ),
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (context, state, child) {
          return MainScaffold(location: state.matchedLocation, child: child);
        },
        routes: [
          GoRoute(
            path: '/catalogo',
            builder: (context, state) => const CatalogoScreen(),
          ),
          GoRoute(
            path: '/mis-citas',
            builder: (context, state) => const MisCitasScreen(),
          ),
          GoRoute(
            path: '/mis-cuotas',
            builder: (context, state) => const MisCuotasScreen(),
          ),
          GoRoute(
            path: '/mis-archivos',
            builder: (context, state) => const DocumentosScreen(),
          ),
          GoRoute(
            path: '/perfil',
            builder: (context, state) => const PerfilScreen(),
          ),
        ],
      ),
    ],
  );

  static Future<String?> _redirect(
    BuildContext context,
    GoRouterState state,
  ) async {
    final hasToken = (await DioClient.getToken()) != null;
    final isPublicRoute = _publicRoutes.contains(state.matchedLocation) ||
        state.matchedLocation.startsWith(_publicPrefix);

    if (!hasToken && !isPublicRoute) {
      return '/login';
    }
    if (hasToken && state.matchedLocation == '/login') {
      return '/catalogo';
    }
    return null;
  }
}
