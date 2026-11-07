import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Persistent scaffold with the bottom navigation bar, used as the
/// [ShellRoute] container for the authenticated tab screens.
class MainScaffold extends StatelessWidget {
  const MainScaffold({super.key, required this.location, required this.child});

  final String location;
  final Widget child;

  static const List<String> _tabRoutes = [
    '/catalogo',
    '/mis-citas',
    '/mis-cuotas',
    '/mis-archivos',
    '/perfil',
  ];

  int get _currentIndex {
    final index = _tabRoutes.indexWhere((route) => location.startsWith(route));
    return index == -1 ? 0 : index;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) => context.go(_tabRoutes[index]),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.directions_car_outlined),
            selectedIcon: Icon(Icons.directions_car),
            label: 'Catálogo',
          ),
          NavigationDestination(
            icon: Icon(Icons.event_outlined),
            selectedIcon: Icon(Icons.event),
            label: 'Mis Citas',
          ),
          NavigationDestination(
            icon: Icon(Icons.payments_outlined),
            selectedIcon: Icon(Icons.payments),
            label: 'Mis Cuotas',
          ),
          NavigationDestination(
            icon: Icon(Icons.folder_outlined),
            selectedIcon: Icon(Icons.folder),
            label: 'Mis Archivos',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Perfil',
          ),
        ],
      ),
    );
  }
}
