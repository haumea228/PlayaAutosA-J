import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/citas_provider.dart';
import '../widgets/cita_card.dart';

class MisCitasScreen extends ConsumerWidget {
  const MisCitasScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final citasAsync = ref.watch(misCitasProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Mis Citas')),
      body: citasAsync.when(
        data: (citas) {
          if (citas.isEmpty) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.event_busy_outlined,
                      size: 48,
                      color: Theme.of(context).colorScheme.outline,
                    ),
                    const SizedBox(height: 12),
                    const Text(
                      'Todavía no tenés citas agendadas.',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 16),
                    FilledButton.icon(
                      onPressed: () => context.push('/agendar-cita'),
                      icon: const Icon(Icons.add),
                      label: const Text('Agendar nueva cita'),
                    ),
                  ],
                ),
              ),
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.refresh(misCitasProvider.future),
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: citas.length,
              separatorBuilder: (context, index) => const SizedBox(height: 12),
              itemBuilder: (context, index) => CitaCard(cita: citas[index]),
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stack) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline, size: 40),
                const SizedBox(height: 12),
                Text(
                  'No se pudieron cargar tus citas.\n$error',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: () => ref.invalidate(misCitasProvider),
                  child: const Text('Reintentar'),
                ),
              ],
            ),
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/agendar-cita'),
        icon: const Icon(Icons.add),
        label: const Text('Agendar nueva cita'),
      ),
    );
  }
}
