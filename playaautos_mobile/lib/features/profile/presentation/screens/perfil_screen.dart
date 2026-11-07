import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:path_provider/path_provider.dart';

import '../../../../config/theme/app_theme.dart';
import '../../../../core/services/camera_service.dart';
import '../../../../core/services/gps_service.dart';
import '../../../auth/presentation/providers/auth_provider.dart';
import '../providers/perfil_provider.dart';

class PerfilScreen extends ConsumerStatefulWidget {
  const PerfilScreen({super.key});

  @override
  ConsumerState<PerfilScreen> createState() => _PerfilScreenState();
}

class _PerfilScreenState extends ConsumerState<PerfilScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nombreController = TextEditingController();
  final _ciRucController = TextEditingController();
  final _telefonoController = TextEditingController();
  final _direccionController = TextEditingController();

  final _camera = CameraService();
  final _gps = GpsService();
  late final Future<String> _direccionAgenciaFuture = _gps
      .getAddressFromCoordinates(agenciaLat, agenciaLng);

  // Qué cliente está reflejado ahora mismo en los controllers — no un simple
  // bool "ya precargado una vez". Si fuera un bool, un logout+login con otra
  // cuenta mientras esta misma instancia de PerfilScreen sigue viva (GoRouter
  // no siempre recrea el árbol del ShellRoute al pasar por /login y volver)
  // dejaba los datos del cliente anterior pisados en pantalla, porque el
  // bool ya estaba en true y el bloque de abajo nunca se volvía a ejecutar.
  // Comparar por id hace que SÍ se vuelva a precargar cuando cambia el
  // cliente, pero sin pisar una edición en curso del mismo cliente cuando
  // perfilProvider simplemente se refresca (ej. después de Guardar cambios).
  int? _clienteIdCargado;
  File? _fotoPerfil;

  @override
  void dispose() {
    _nombreController.dispose();
    _ciRucController.dispose();
    _telefonoController.dispose();
    _direccionController.dispose();
    super.dispose();
  }

  Future<File> _archivoFotoPerfil(int clienteId) async {
    final dir = await getApplicationDocumentsDirectory();
    return File('${dir.path}/perfil_$clienteId.jpg');
  }

  // Al reabrir la app se carga la última foto guardada para ese cliente; no
  // hay atributo "fotoPerfil" en el backend, así que queda solo en el
  // dispositivo (no se sincroniza entre dispositivos ni se sube al servidor).
  Future<void> _cargarFotoGuardada(int clienteId) async {
    final archivo = await _archivoFotoPerfil(clienteId);
    if (await archivo.exists() && mounted) {
      setState(() => _fotoPerfil = archivo);
    }
  }

  Future<void> _elegirFoto({required bool deCamara}) async {
    Navigator.of(context).pop();
    final seleccionada = deCamara
        ? await _camera.pickImageFromCamera()
        : await _camera.pickImageFromGallery();
    if (seleccionada == null) return;

    // Se muestra directo el archivo que devuelve image_picker (path nuevo en
    // cada toma/selección) en vez de releer el archivo persistido: evita que
    // Image/FileImage sirvan una versión cacheada del mismo path al pisarlo.
    setState(() => _fotoPerfil = seleccionada);

    final clienteId = ref.read(perfilProvider).valueOrNull?.id;
    if (clienteId == null) return;
    final destino = await _archivoFotoPerfil(clienteId);
    await seleccionada.copy(destino.path);
  }

  void _mostrarSelectorFoto() {
    showModalBottomSheet<void>(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Tomar foto'),
              onTap: () => _elegirFoto(deCamara: true),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Elegir de la galería'),
              onTap: () => _elegirFoto(deCamara: false),
            ),
          ],
        ),
      ),
    );
  }

  void _guardar() {
    if (!_formKey.currentState!.validate()) return;
    final actual = ref.read(perfilProvider).valueOrNull;
    if (actual == null) return;

    ref
        .read(actualizarPerfilProvider.notifier)
        .actualizar(
          actual.copyWith(
            nombre: _nombreController.text.trim(),
            telefono: _telefonoController.text.trim(),
            direccion: _direccionController.text.trim(),
          ),
        );
  }

  Future<void> _abrirUbicacionAgencia() async {
    try {
      await _gps.openMapsToAgency();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('No se pudo abrir Maps.')));
      }
    }
  }

  Future<void> _cerrarSesion() async {
    final confirmar = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cerrar sesión'),
        content: const Text('¿Querés cerrar tu sesión?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Cerrar sesión'),
          ),
        ],
      ),
    );
    if (confirmar != true) return;

    await ref.read(authProvider.notifier).logout();
    if (mounted) context.go('/login');
  }

  @override
  Widget build(BuildContext context) {
    final perfilAsync = ref.watch(perfilProvider);
    final actualizarState = ref.watch(actualizarPerfilProvider);

    // Se precarga a partir del valor YA disponible en vez de esperar un
    // cambio vía ref.listen: MainScaffold reconstruye esta pantalla de cero
    // cada vez que se vuelve a la pestaña Perfil (no usa IndexedStack), así
    // que al volver, perfilProvider suele seguir teniendo el dato en caché y
    // ref.listen nunca dispara (no hay transición, el valor ya estaba
    // asentado) — los campos quedaban vacíos. Chequear el valor actual en
    // cada build cubre tanto la primera carga como estos reingresos.
    //
    // Comparar por clienteId (no un bool de una sola vez) también cubre
    // cambiar de cuenta: si vuelve a aparecer esta misma instancia después
    // de un logout+login con otro cliente, perfilCargado.id ya no coincide
    // con _clienteIdCargado y se vuelve a precargar con los datos nuevos.
    final perfilCargado = perfilAsync.valueOrNull;
    if (perfilCargado != null && _clienteIdCargado != perfilCargado.id) {
      _clienteIdCargado = perfilCargado.id;
      _nombreController.text = perfilCargado.nombre;
      _ciRucController.text = (perfilCargado.ciRuc == null || perfilCargado.ciRuc!.isEmpty)
          ? 'Sin registrar'
          : perfilCargado.ciRuc!;
      _telefonoController.text = perfilCargado.telefono ?? '';
      _direccionController.text = perfilCargado.direccion ?? '';
      _fotoPerfil = null;
      _cargarFotoGuardada(perfilCargado.id);
    }

    ref.listen<ActualizarPerfilState>(actualizarPerfilProvider, (
      previous,
      next,
    ) {
      if (!context.mounted) return;
      if (next.error != null) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next.error!)));
      } else if (next.success && previous?.success != true) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Perfil actualizado correctamente.')),
        );
      }
    });

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              width: 38,
              height: 38,
              padding: const EdgeInsets.all(5),
              decoration: const BoxDecoration(
                color: AppColors.offWhiteBeige,
                shape: BoxShape.circle,
              ),
              child: Image.asset(
                'assets/images/logo.png',
                fit: BoxFit.contain,
                cacheWidth: 100,
              ),
            ),
            const SizedBox(width: 10),
            const Text('Mi perfil'),
          ],
        ),
      ),
      body: perfilAsync.when(
        data: (perfil) => GestureDetector(
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
                    Center(
                      child: Stack(
                        children: [
                          CircleAvatar(
                            radius: 52,
                            backgroundColor: Theme.of(
                              context,
                            ).colorScheme.secondaryContainer,
                            backgroundImage: _fotoPerfil != null
                                ? FileImage(_fotoPerfil!)
                                : null,
                            child: _fotoPerfil == null
                                ? Icon(
                                    Icons.person,
                                    size: 52,
                                    color: Theme.of(
                                      context,
                                    ).colorScheme.onSecondaryContainer,
                                  )
                                : null,
                          ),
                          Positioned(
                            right: 0,
                            bottom: 0,
                            child: Material(
                              color: Theme.of(context).colorScheme.primary,
                              shape: const CircleBorder(),
                              child: InkWell(
                                customBorder: const CircleBorder(),
                                onTap: _mostrarSelectorFoto,
                                child: const Padding(
                                  padding: EdgeInsets.all(8),
                                  child: Icon(
                                    Icons.camera_alt_outlined,
                                    size: 18,
                                    color: Colors.white,
                                  ),
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                    Text(
                      'Tus datos',
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
                      controller: _ciRucController,
                      readOnly: true,
                      enabled: false,
                      decoration: const InputDecoration(
                        labelText: 'CI / RUC',
                        helperText: 'No editable',
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
                    const SizedBox(height: 16),
                    FilledButton(
                      onPressed: actualizarState.isLoading ? null : _guardar,
                      child: actualizarState.isLoading
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Text('Guardar cambios'),
                    ),
                    const SizedBox(height: 24),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Icon(
                                  Icons.store_outlined,
                                  color: Theme.of(context).colorScheme.primary,
                                ),
                                const SizedBox(width: 8),
                                Text(
                                  'Ubicación de la agencia',
                                  style: Theme.of(context).textTheme.titleSmall,
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            Text(agenciaNombre),
                            FutureBuilder<String>(
                              future: _direccionAgenciaFuture,
                              builder: (context, snapshot) {
                                if (snapshot.connectionState !=
                                        ConnectionState.done ||
                                    snapshot.hasError ||
                                    (snapshot.data ?? '').isEmpty) {
                                  return const SizedBox.shrink();
                                }
                                return Padding(
                                  padding: const EdgeInsets.only(top: 2),
                                  child: Text(
                                    snapshot.data!,
                                    style: Theme.of(context)
                                        .textTheme
                                        .bodySmall
                                        ?.copyWith(
                                          color: Theme.of(
                                            context,
                                          ).colorScheme.outline,
                                        ),
                                  ),
                                );
                              },
                            ),
                            const SizedBox(height: 12),
                            OutlinedButton.icon(
                              onPressed: _abrirUbicacionAgencia,
                              icon: const Icon(Icons.map_outlined),
                              label: const Text('Ver ubicación de la agencia'),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    OutlinedButton.icon(
                      onPressed: _cerrarSesion,
                      icon: const Icon(Icons.logout),
                      label: const Text('Cerrar sesión'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: Theme.of(context).colorScheme.error,
                        side: BorderSide(
                          color: Theme.of(context).colorScheme.error,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
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
                  'No se pudo cargar tu perfil.\n$error',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: () => ref.invalidate(perfilProvider),
                  child: const Text('Reintentar'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
