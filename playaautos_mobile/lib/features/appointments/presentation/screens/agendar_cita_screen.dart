import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../catalog/presentation/providers/catalogo_provider.dart';
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
  final _formKey = GlobalKey<FormState>();
  final _nombreController = TextEditingController();
  final _ciController = TextEditingController();
  final _telefonoController = TextEditingController();
  final _direccionController = TextEditingController();
  final _observacionesController = TextEditingController();

  DateTime? _fecha;
  String? _horario;
  // Qué cliente está reflejado en los controllers — no un bool de una sola
  // vez (ver nota larga en perfil_screen.dart: un bool deja datos del
  // cliente anterior pisados en pantalla si esta instancia sobrevive a un
  // logout+login con otra cuenta).
  int? _clienteIdCargado;
  bool _ciYaRegistrado = false;

  int? get _vehiculoIdInt {
    final id = widget.vehiculoId;
    return id == null ? null : int.tryParse(id);
  }

  @override
  void dispose() {
    _nombreController.dispose();
    _ciController.dispose();
    _telefonoController.dispose();
    _direccionController.dispose();
    _observacionesController.dispose();
    super.dispose();
  }

  Future<void> _elegirFecha() async {
    final ahora = DateTime.now();
    final primerDiaValido = ahora.add(const Duration(days: 1));
    final fecha = await showDatePicker(
      context: context,
      initialDate: primerDiaValido,
      firstDate: ahora,
      lastDate: ahora.add(const Duration(days: 90)),
      selectableDayPredicate: (date) => date.weekday != DateTime.sunday,
    );
    if (fecha != null) {
      setState(() {
        _fecha = fecha;
        _horario = null;
      });
    }
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) return;
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
          nombre: _nombreController.text.trim(),
          telefono: _telefonoController.text.trim(),
          direccion: _direccionController.text.trim(),
          ciRuc: _ciYaRegistrado ? null : _ciController.text.trim(),
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

    // Primera vez que agenda: el perfil viene con nombre/CI/telefono/direccion
    // vacíos y el cliente los completa acá (igual que en la web). Si ya
    // agendó antes, se precargan y quedan editables — salvo el CI/RUC, que
    // una vez registrado el backend ya no lo vuelve a pisar.
    //
    // Se precarga a partir del valor YA disponible en perfilProvider en vez
    // de esperar un cambio vía ref.listen: si el cliente ya visitó Perfil
    // antes, perfilProvider llega acá con el dato cacheado y ref.listen no
    // dispara (no hay transición, el valor ya estaba asentado) — los campos
    // quedaban vacíos pese a haber datos.
    final perfilCargado = ref.watch(perfilProvider).valueOrNull;
    if (perfilCargado != null && _clienteIdCargado != perfilCargado.id) {
      _clienteIdCargado = perfilCargado.id;
      _nombreController.text = perfilCargado.nombre;
      _telefonoController.text = perfilCargado.telefono ?? '';
      _direccionController.text = perfilCargado.direccion ?? '';
      _ciController.text = perfilCargado.ciRuc ?? '';
      _ciYaRegistrado =
          perfilCargado.ciRuc != null && perfilCargado.ciRuc!.isNotEmpty;
    }

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
              key: _formKey,
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
                  TextFormField(
                    controller: _nombreController,
                    decoration: const InputDecoration(
                      labelText: 'Nombre completo',
                    ),
                    validator: (value) =>
                        (value == null || value.trim().isEmpty)
                        ? 'Ingresá tu nombre'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _ciController,
                    readOnly: _ciYaRegistrado,
                    decoration: InputDecoration(
                      labelText: 'CI / RUC',
                      helperText: _ciYaRegistrado
                          ? 'Ya registrado, no editable'
                          : null,
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _telefonoController,
                    keyboardType: TextInputType.phone,
                    decoration: const InputDecoration(labelText: 'Teléfono'),
                    validator: (value) =>
                        (value == null || value.trim().isEmpty)
                        ? 'Ingresá tu teléfono'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _direccionController,
                    decoration: const InputDecoration(labelText: 'Dirección'),
                    validator: (value) =>
                        (value == null || value.trim().isEmpty)
                        ? 'Ingresá tu dirección'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _observacionesController,
                    decoration: const InputDecoration(
                      labelText: 'Observaciones (opcional)',
                    ),
                    maxLines: 2,
                  ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: agendarState.isLoading ? null : _submit,
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
