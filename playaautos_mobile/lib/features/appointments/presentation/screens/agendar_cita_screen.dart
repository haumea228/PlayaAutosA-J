import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../catalog/presentation/providers/catalogo_provider.dart';
import '../../../profile/data/models/cliente_model.dart';
import '../../../profile/presentation/providers/perfil_provider.dart';
import '../providers/citas_provider.dart';

class AgendarCitaScreen extends ConsumerStatefulWidget {
  const AgendarCitaScreen({super.key, this.vehiculoId});

  /// Nulo cuando se agenda una consulta general desde "Mis Citas" (sin
  /// pasar por el detalle de un vehículo puntual).
  final String? vehiculoId;

  @override
  ConsumerState<AgendarCitaScreen> createState() => _AgendarCitaScreenState();
}

class _AgendarCitaScreenState extends ConsumerState<AgendarCitaScreen> {
  final _observacionesController = TextEditingController();

  DateTime? _fecha;
  String? _horario;

  int? get _vehiculoIdInt {
    final id = widget.vehiculoId;
    return id == null ? null : int.tryParse(id);
  }

  @override
  void dispose() {
    _observacionesController.dispose();
    super.dispose();
  }

  /// Los datos de contacto se editan solo en Perfil. Para agendar, el perfil
  /// necesita al menos nombre y teléfono (la API también lo exige).
  static bool _perfilCompleto(ClienteModel? perfil) =>
      perfil != null &&
      perfil.nombre.trim().isNotEmpty &&
      (perfil.telefono ?? '').trim().isNotEmpty;

  static bool _esDiaHabil(DateTime fecha) => fecha.weekday != DateTime.sunday;

  Future<void> _elegirFecha() async {
    final hoy = DateUtils.dateOnly(DateTime.now());

    // La fecha inicial del calendario TIENE que cumplir selectableDayPredicate
    // (si no, showDatePicker lanza una aserción y el botón "no hace nada").
    // Antes era siempre "mañana", que un sábado cae en domingo: se busca el
    // próximo día hábil.
    var inicial = _fecha ?? hoy.add(const Duration(days: 1));
    while (!_esDiaHabil(inicial)) {
      inicial = inicial.add(const Duration(days: 1));
    }

    final fecha = await showDatePicker(
      context: context,
      initialDate: inicial,
      firstDate: hoy,
      lastDate: hoy.add(const Duration(days: 90)),
      selectableDayPredicate: _esDiaHabil,
    );
    if (fecha != null) {
      setState(() {
        _fecha = fecha;
        _horario = null;
      });
    }
  }

  void _submit() {
    if (!_perfilCompleto(ref.read(perfilProvider).valueOrNull)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Completá tu nombre y teléfono en tu perfil.')),
      );
      return;
    }
    if (_fecha == null || _horario == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Elegí una fecha y un horario.')),
      );
      return;
    }

    final partes = _horario!.split(':');
    final fechaHora = DateTime(
      _fecha!.year,
      _fecha!.month,
      _fecha!.day,
      int.parse(partes[0]),
      int.parse(partes[1]),
    );

    ref
        .read(agendarCitaProvider.notifier)
        .agendar(
          vehiculoId: _vehiculoIdInt,
          fechaHora: fechaHora,
          observaciones: _observacionesController.text.trim().isEmpty
              ? null
              : _observacionesController.text.trim(),
        );
  }

  @override
  Widget build(BuildContext context) {
    final vehiculoIdInt = _vehiculoIdInt;
    final vehiculoAsync = vehiculoIdInt != null
        ? ref.watch(detalleVehiculoProvider(vehiculoIdInt))
        : null;
    final horariosAsync = _fecha != null
        ? ref.watch(horariosDisponiblesProvider(_fecha!))
        : null;
    final agendarState = ref.watch(agendarCitaProvider);
    // Se observa el perfil: al volver de "Editar en Perfil" (que lo refresca
    // al guardar) la tarjeta de contacto se actualiza sola.
    final perfilAsync = ref.watch(perfilProvider);
    final perfilCompleto = _perfilCompleto(perfilAsync.valueOrNull);

    ref.listen<AgendarCitaState>(agendarCitaProvider, (previous, next) {
      if (!context.mounted) return;
      if (next.error != null) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(next.error!)));
      } else if (next.citaId != null && previous?.citaId != next.citaId) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Cita agendada correctamente.')),
        );
        context.go('/mis-citas');
      }
    });

    return Scaffold(
      appBar: AppBar(title: const Text('Agendar cita')),
      body: GestureDetector(
        behavior: HitTestBehavior.translucent,
        onTap: () => FocusScope.of(context).unfocus(),
        child: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Form(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (vehiculoAsync != null)
                    vehiculoAsync.when(
                      data: (v) => Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            children: [
                              Icon(
                                Icons.directions_car,
                                color: Theme.of(context).colorScheme.primary,
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Text(
                                  '${v.marcaNombre} ${v.modeloNombre} · ${v.anio}',
                                  style: Theme.of(context).textTheme.titleSmall
                                      ?.copyWith(fontWeight: FontWeight.bold),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      loading: () => const LinearProgressIndicator(),
                      error: (error, stack) => const SizedBox.shrink(),
                    ),
                  const SizedBox(height: 20),
                  Text(
                    'Fecha y horario',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    onPressed: _elegirFecha,
                    icon: const Icon(Icons.calendar_month_outlined),
                    label: Text(
                      _fecha == null
                          ? 'Elegir fecha'
                          : DateFormat('dd/MM/yyyy').format(_fecha!),
                    ),
                  ),
                  const SizedBox(height: 12),
                  if (_fecha != null)
                    horariosAsync!.when(
                      data: (horarios) {
                        if (horarios.isEmpty) {
                          return const Text(
                            'No hay horarios disponibles ese día.',
                          );
                        }
                        return Wrap(
                          spacing: 8,
                          runSpacing: 8,
                          children: [
                            for (final horario in horarios)
                              ChoiceChip(
                                label: Text(horario),
                                selected: _horario == horario,
                                onSelected: (_) =>
                                    setState(() => _horario = horario),
                              ),
                          ],
                        );
                      },
                      loading: () =>
                          const Center(child: CircularProgressIndicator()),
                      error: (error, stack) =>
                          const Text('No se pudieron cargar los horarios.'),
                    ),
                  const SizedBox(height: 24),
                  Text(
                    'Tus datos de contacto',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 8),
                  perfilAsync.when(
                    data: (perfil) => _DatosContacto(
                      perfil: perfil,
                      completo: perfilCompleto,
                      onEditar: () => context.push('/editar-perfil'),
                    ),
                    loading: () => const LinearProgressIndicator(),
                    error: (error, stack) => _DatosContacto(
                      perfil: null,
                      completo: false,
                      onEditar: () => ref.invalidate(perfilProvider),
                      error: true,
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _observacionesController,
                    decoration: const InputDecoration(
                      labelText: 'Observaciones (opcional)',
                    ),
                    maxLines: 2,
                  ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: agendarState.isLoading || !perfilCompleto ? null : _submit,
                    child: agendarState.isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Text('Confirmar cita'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Datos de contacto del cliente, de solo lectura: se editan únicamente en
/// Perfil (donde viven las reglas, como el CI/RUC de carga única).
class _DatosContacto extends StatelessWidget {
  const _DatosContacto({
    required this.perfil,
    required this.completo,
    required this.onEditar,
    this.error = false,
  });

  final ClienteModel? perfil;
  final bool completo;
  final VoidCallback onEditar;
  final bool error;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    if (error) {
      return Row(
        children: [
          const Expanded(child: Text('No se pudieron cargar tus datos.')),
          TextButton(onPressed: onEditar, child: const Text('Reintentar')),
        ],
      );
    }

    final p = perfil!;
    String valor(String? v) => (v ?? '').trim().isEmpty ? '—' : v!.trim();

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Te vamos a contactar con estos datos:',
              style: theme.textTheme.bodySmall?.copyWith(color: colors.outline),
            ),
            const SizedBox(height: 8),
            _Fila(icono: Icons.person_outline, texto: valor(p.nombre)),
            _Fila(icono: Icons.phone_outlined, texto: valor(p.telefono)),
            if ((p.direccion ?? '').trim().isNotEmpty)
              _Fila(icono: Icons.home_outlined, texto: p.direccion!.trim()),
            if (!completo) ...[
              const SizedBox(height: 10),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: colors.error.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  'Para agendar necesitamos tu nombre y teléfono. Completalos en tu perfil.',
                  style: theme.textTheme.bodySmall?.copyWith(color: colors.error),
                ),
              ),
            ],
            const SizedBox(height: 6),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton.icon(
                onPressed: onEditar,
                icon: const Icon(Icons.edit_outlined, size: 18),
                label: Text(completo ? 'Editar en Perfil' : 'Completar en Perfil'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Fila extends StatelessWidget {
  const _Fila({required this.icono, required this.texto});

  final IconData icono;
  final String texto;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Icon(icono, size: 18, color: Theme.of(context).colorScheme.primary),
          const SizedBox(width: 10),
          Expanded(child: Text(texto)),
        ],
      ),
    );
  }
}
