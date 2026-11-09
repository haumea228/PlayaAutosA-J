import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:path_provider/path_provider.dart';

import '../../../../config/theme/app_theme.dart';
import '../../../../core/config/contacto.dart';
import '../../../../core/services/camera_service.dart';
import '../../../../core/services/gps_service.dart';
import '../../../../shared/utils/validators.dart';
import '../../../auth/presentation/providers/auth_provider.dart';
import '../../data/models/cliente_model.dart';
import '../../data/profile_repository.dart';
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
  final _emailController = TextEditingController();
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
    _emailController.dispose();
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

  Future<void> _guardar() async {
    if (!_formKey.currentState!.validate()) return;
    final actual = ref.read(perfilProvider).valueOrNull;
    if (actual == null) return;

    // El CI/RUC se carga una única vez: si el cliente lo está cargando
    // ahora, se le pide que lo revise antes de guardarlo.
    String? ciNuevo;
    if (!actual.tieneCiRuc) {
      final ci = Validators.normalizarCiRuc(_ciRucController.text);
      if (ci.isNotEmpty) {
        final confirmar = await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Confirmá tu CI / RUC'),
            content: Text(
              'Vas a registrar el CI/RUC $ci.\n\n'
              'Revisalo bien: una vez guardado no vas a poder cambiarlo desde la '
              'app. Si después necesitás corregirlo, tendrás que pedírselo a la playa.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Revisar'),
              ),
              FilledButton(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Guardar'),
              ),
            ],
          ),
        );
        if (confirmar != true || !mounted) return;
        ciNuevo = ci;
      }
    }

    // El email es también el usuario para iniciar sesión: si cambia, se pide
    // la contraseña actual (la API la verifica) antes de guardar.
    final emailNuevo = _emailController.text.trim();
    final cambiaEmail =
        emailNuevo.toLowerCase() != (actual.email ?? '').trim().toLowerCase();
    String? passwordActual;
    if (cambiaEmail) {
      passwordActual = await _pedirContrasena(emailNuevo);
      if (passwordActual == null || !mounted) return;
    }

    ref
        .read(actualizarPerfilProvider.notifier)
        .actualizar(
          actual.copyWith(
            nombre: _nombreController.text.trim(),
            ciRuc: ciNuevo,
            telefono: _telefonoController.text.trim(),
            email: emailNuevo,
            direccion: _direccionController.text.trim(),
          ),
          passwordActual: passwordActual,
        );
  }

  /// Confirmación del cambio de email con la contraseña actual. Devuelve la
  /// contraseña, o null si el cliente cancela.
  Future<String?> _pedirContrasena(String emailNuevo) {
    return showDialog<String>(
      context: context,
      builder: (_) => _DialogoContrasena(emailNuevo: emailNuevo),
    );
  }

  Future<void> _solicitarCorreccionCi(ClienteModel perfil) async {
    final abierto = await Contacto.abrirEmail(
      asunto: 'Corrección de CI/RUC — Cliente N° ${perfil.id}',
      cuerpo:
          'Hola, solicito la corrección de mi CI/RUC.\n\n'
          'Nombre: ${perfil.nombre}\n'
          'Email de la cuenta: ${perfil.email ?? '-'}\n'
          'CI/RUC registrado: ${perfil.ciRuc}\n'
          'CI/RUC correcto: \n\n'
          'Gracias.',
    );
    if (!abierto) {
      _avisoSinApp(
        'No se encontró una app de correo. Escribinos a ${Contacto.email}.',
      );
    }
  }

  Future<void> _abrirWhatsApp() async {
    final abierto = await Contacto.abrirWhatsApp(
      mensaje: 'Hola, les escribo desde la app de A&J Autos Usados.',
    );
    if (!abierto) {
      _avisoSinApp(
        'No se pudo abrir WhatsApp. Nuestro número es ${Contacto.telefono}.',
      );
    }
  }

  Future<void> _abrirEmailContacto() async {
    final abierto = await Contacto.abrirEmail(asunto: 'Consulta desde la app');
    if (!abierto) {
      _avisoSinApp(
        'No se encontró una app de correo. Escribinos a ${Contacto.email}.',
      );
    }
  }

  void _avisoSinApp(String mensaje) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(mensaje)));
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

  Future<void> _cambiarContrasena() async {
    final cambiada = await showDialog<bool>(
      context: context,
      builder: (_) => _DialogoCambiarContrasena(
        repositorio: ref.read(profileRepositoryProvider),
      ),
    );
    if (cambiada == true && mounted) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Contraseña actualizada.')));
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
      _ciRucController.text = perfilCargado.ciRuc ?? '';
      _telefonoController.text = perfilCargado.telefono ?? '';
      _emailController.text = perfilCargado.email ?? '';
      _direccionController.text = perfilCargado.direccion ?? '';
      _fotoPerfil = null;
      _cargarFotoGuardada(perfilCargado.id);
    }
    // Al registrar el CI se muestra el valor guardado (normalizado por la
    // API, p. ej. sin puntos). Ya bloqueado, no pisa ninguna edición.
    if (perfilCargado != null &&
        perfilCargado.tieneCiRuc &&
        _ciRucController.text != perfilCargado.ciRuc) {
      _ciRucController.text = perfilCargado.ciRuc!;
    }

    ref.listen<ActualizarPerfilState>(actualizarPerfilProvider, (
      previous,
      next,
    ) {
      if (!context.mounted) return;
      if (next.error != null) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(next.error!)));
      } else if (next.success && previous?.success != true) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              next.emailCambiado
                  ? 'Perfil actualizado. Desde ahora iniciá sesión con tu nuevo email.'
                  : 'Perfil actualizado correctamente.',
            ),
          ),
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
                            backgroundColor: Theme.of(context)
                                .colorScheme
                                .secondaryContainer,
                            backgroundImage: _fotoPerfil != null
                                ? FileImage(_fotoPerfil!)
                                : null,
                            child: _fotoPerfil == null
                                ? Icon(
                                    Icons.person,
                                    size: 52,
                                    color: Theme.of(context)
                                        .colorScheme
                                        .onSecondaryContainer,
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
                      readOnly: perfil.tieneCiRuc,
                      enabled: !perfil.tieneCiRuc,
                      keyboardType: TextInputType.text,
                      decoration: InputDecoration(
                        labelText: 'CI / RUC',
                        hintText: 'Ej.: 1234567 o 80012345-6',
                        helperText: perfil.tieneCiRuc
                            ? 'Ya registrado: no se puede modificar desde la app'
                            : 'Podés cargarlo una sola vez. Revisalo bien antes de guardar.',
                        helperMaxLines: 2,
                        errorMaxLines: 2,
                        prefixIcon: perfil.tieneCiRuc
                            ? const Icon(Icons.lock_outline, size: 20)
                            : null,
                      ),
                      validator: perfil.tieneCiRuc ? null : Validators.ciRuc,
                    ),
                    if (perfil.tieneCiRuc)
                      Align(
                        alignment: Alignment.centerLeft,
                        child: TextButton.icon(
                          onPressed: () => _solicitarCorreccionCi(perfil),
                          icon: const Icon(Icons.mail_outline, size: 18),
                          label: const Text(
                            '¿Tu CI es incorrecto? Solicitar corrección',
                          ),
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
                      controller: _emailController,
                      keyboardType: TextInputType.emailAddress,
                      autocorrect: false,
                      decoration: const InputDecoration(
                        labelText: 'Email',
                        helperText:
                            'Es también tu usuario para iniciar sesión. '
                            'Para cambiarlo te vamos a pedir tu contraseña.',
                        helperMaxLines: 2,
                      ),
                      validator: Validators.email,
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
                                    style: Theme.of(context).textTheme.bodySmall
                                        ?.copyWith(
                                          color: Theme.of(context)
                                              .colorScheme
                                              .outline,
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
                    const SizedBox(height: 12),
                    _AcercaDeNosotros(
                      onWhatsApp: _abrirWhatsApp,
                      onEmail: _abrirEmailContacto,
                    ),
                    const SizedBox(height: 24),
                    OutlinedButton.icon(
                      onPressed: _cambiarContrasena,
                      icon: const Icon(Icons.password),
                      label: const Text('Cambiar contraseña'),
                    ),
                    const SizedBox(height: 8),
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

/// "Acerca de nosotros": presentación breve de la playa, contacto por
/// WhatsApp (abre directo el chat) y email, y cómo pedir correcciones de datos.
class _AcercaDeNosotros extends StatelessWidget {
  const _AcercaDeNosotros({required this.onWhatsApp, required this.onEmail});

  final VoidCallback onWhatsApp;
  final VoidCallback onEmail;

  static const Color _verdeWhatsApp = Color(0xFF25D366);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.info_outline, color: colors.primary),
                const SizedBox(width: 8),
                Text('Acerca de nosotros', style: theme.textTheme.titleSmall),
              ],
            ),
            const SizedBox(height: 10),
            Text(
              '$agenciaNombre es una playa de autos de Carapeguá dedicada a la '
              'compra, venta y consignación de vehículos usados, con la opción '
              'de financiar tu compra en cuotas.',
              style: theme.textTheme.bodyMedium,
            ),
            const SizedBox(height: 14),
            _FilaContacto(
              icono: Icons.phone_outlined,
              texto: Contacto.telefono,
            ),
            const SizedBox(height: 4),
            _FilaContacto(icono: Icons.mail_outline, texto: Contacto.email),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: FilledButton.icon(
                    onPressed: onWhatsApp,
                    style: FilledButton.styleFrom(
                      backgroundColor: _verdeWhatsApp,
                      foregroundColor: Colors.white,
                    ),
                    icon: const Icon(Icons.chat_outlined),
                    label: const Text('WhatsApp'),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: onEmail,
                    icon: const Icon(Icons.mail_outline),
                    label: const Text('Email'),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: colors.primary.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Text(
                '¿Necesitás corregir un dato que no podés editar, como tu CI/RUC? '
                'Escribinos a ${Contacto.email} indicando el dato correcto y lo '
                'actualizamos por vos.',
                style: theme.textTheme.bodySmall,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _FilaContacto extends StatelessWidget {
  const _FilaContacto({required this.icono, required this.texto});

  final IconData icono;
  final String texto;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Row(
      children: [
        Icon(icono, size: 18, color: colors.outline),
        const SizedBox(width: 8),
        Expanded(child: SelectableText(texto)),
      ],
    );
  }
}

/// Pide la contraseña actual para confirmar el cambio de email (el email es
/// el usuario para iniciar sesión). Devuelve la contraseña o null si se cancela.
class _DialogoContrasena extends StatefulWidget {
  const _DialogoContrasena({required this.emailNuevo});

  final String emailNuevo;

  @override
  State<_DialogoContrasena> createState() => _DialogoContrasenaState();
}

class _DialogoContrasenaState extends State<_DialogoContrasena> {
  final _password = TextEditingController();
  bool _oculta = true;

  @override
  void dispose() {
    _password.dispose();
    super.dispose();
  }

  void _confirmar() {
    if (_password.text.isEmpty) return;
    Navigator.of(context).pop(_password.text);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Cambiar email'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Tu nuevo email será ${widget.emailNuevo}. Desde ahora vas a '
              'iniciar sesión con él. Para confirmar, ingresá tu contraseña actual.',
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _password,
              obscureText: _oculta,
              autofocus: true,
              onSubmitted: (_) => _confirmar(),
              decoration: InputDecoration(
                labelText: 'Contraseña actual',
                border: const OutlineInputBorder(),
                suffixIcon: IconButton(
                  tooltip: _oculta ? 'Mostrar' : 'Ocultar',
                  icon: Icon(
                    _oculta
                        ? Icons.visibility_outlined
                        : Icons.visibility_off_outlined,
                  ),
                  onPressed: () => setState(() => _oculta = !_oculta),
                ),
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          style: FilledButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 20),
          ),
          onPressed: _confirmar,
          child: const Text('Confirmar'),
        ),
      ],
    );
  }
}

/// Cambio de contraseña: actual + nueva (dos veces). Envía el cambio y
/// muestra el error de la API en el mismo diálogo. Devuelve true si se cambió.
class _DialogoCambiarContrasena extends StatefulWidget {
  const _DialogoCambiarContrasena({required this.repositorio});

  final ProfileRepository repositorio;

  @override
  State<_DialogoCambiarContrasena> createState() =>
      _DialogoCambiarContrasenaState();
}

class _DialogoCambiarContrasenaState extends State<_DialogoCambiarContrasena> {
  final _form = GlobalKey<FormState>();
  final _actual = TextEditingController();
  final _nueva = TextEditingController();
  final _confirmacion = TextEditingController();
  bool _oculta = true;
  bool _guardando = false;
  String? _error;

  @override
  void dispose() {
    _actual.dispose();
    _nueva.dispose();
    _confirmacion.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _guardando = true;
      _error = null;
    });
    try {
      await widget.repositorio.cambiarContrasena(
        actual: _actual.text,
        nueva: _nueva.text,
      );
      if (mounted) Navigator.of(context).pop(true);
    } on DioException catch (e) {
      if (mounted) {
        setState(
          () => _error = e.message ?? 'No se pudo cambiar la contraseña.',
        );
      }
    } finally {
      if (mounted) setState(() => _guardando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return AlertDialog(
      title: const Text('Cambiar contraseña'),
      content: SingleChildScrollView(
        child: Form(
          key: _form,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: _actual,
                obscureText: _oculta,
                decoration: InputDecoration(
                  labelText: 'Contraseña actual',
                  suffixIcon: IconButton(
                    tooltip: _oculta ? 'Mostrar' : 'Ocultar',
                    icon: Icon(
                      _oculta
                          ? Icons.visibility_outlined
                          : Icons.visibility_off_outlined,
                    ),
                    onPressed: () => setState(() => _oculta = !_oculta),
                  ),
                ),
                validator: (v) =>
                    (v ?? '').isEmpty ? 'Ingresá tu contraseña actual' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _nueva,
                obscureText: _oculta,
                decoration: const InputDecoration(
                  labelText: 'Nueva contraseña',
                ),
                validator: Validators.password,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _confirmacion,
                obscureText: _oculta,
                decoration: const InputDecoration(
                  labelText: 'Repetir contraseña',
                ),
                validator: (v) =>
                    v == _nueva.text ? null : 'Las contraseñas no coinciden',
              ),
              if (_error != null) ...[
                const SizedBox(height: 12),
                Text(_error!, style: TextStyle(color: colors.error)),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _guardando ? null : () => Navigator.of(context).pop(false),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          style: FilledButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 20),
          ),
          onPressed: _guardando ? null : _guardar,
          child: _guardando
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                )
              : const Text('Guardar'),
        ),
      ],
    );
  }
}
