import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

import '../../../../shared/utils/validators.dart';
import '../../data/auth_repository.dart';

/// "¿Olvidaste tu contraseña?" en dos pasos:
/// 1. el cliente escribe su email y la API le envía un código de 6 dígitos;
/// 2. ingresa ese código y la nueva contraseña.
class RecuperarContrasenaScreen extends StatefulWidget {
  const RecuperarContrasenaScreen({super.key, this.emailInicial});

  /// Email ya escrito en el login, para no tener que tipearlo de nuevo.
  final String? emailInicial;

  @override
  State<RecuperarContrasenaScreen> createState() =>
      _RecuperarContrasenaScreenState();
}

class _RecuperarContrasenaScreenState extends State<RecuperarContrasenaScreen> {
  final _repositorio = AuthRepository();
  final _formEmail = GlobalKey<FormState>();
  final _formCodigo = GlobalKey<FormState>();
  late final _email = TextEditingController(text: widget.emailInicial ?? '');
  final _codigo = TextEditingController();
  final _nueva = TextEditingController();
  final _confirmacion = TextEditingController();

  bool _codigoEnviado = false;
  bool _cargando = false;
  bool _oculta = true;

  @override
  void dispose() {
    _email.dispose();
    _codigo.dispose();
    _nueva.dispose();
    _confirmacion.dispose();
    super.dispose();
  }

  void _snack(String mensaje) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(mensaje)));
  }

  Future<void> _enviarCodigo() async {
    if (!_formEmail.currentState!.validate()) return;
    setState(() => _cargando = true);
    try {
      final mensaje = await _repositorio.solicitarCodigoRecuperacion(
        _email.text.trim(),
      );
      if (!mounted) return;
      setState(() => _codigoEnviado = true);
      _snack(mensaje);
    } on DioException catch (e) {
      _snack(e.message ?? 'No se pudo enviar el código.');
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  Future<void> _restablecer() async {
    if (!_formCodigo.currentState!.validate()) return;
    setState(() => _cargando = true);
    try {
      await _repositorio.restablecerContrasena(
        email: _email.text.trim(),
        codigo: _codigo.text.trim(),
        nuevaPassword: _nueva.text,
      );
      if (!mounted) return;
      _snack('Listo, ya podés iniciar sesión con tu nueva contraseña.');
      context.pop();
    } on DioException catch (e) {
      _snack(e.message ?? 'No se pudo cambiar la contraseña.');
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Recuperar contraseña')),
      body: GestureDetector(
        behavior: HitTestBehavior.translucent,
        onTap: () => FocusScope.of(context).unfocus(),
        child: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  _codigoEnviado
                      ? 'Revisá tu correo (también la carpeta de spam) e ingresá '
                            'el código de 6 dígitos que te enviamos. Vence en 15 minutos.'
                      : 'Escribí el email de tu cuenta y te enviamos un código '
                            'para crear una contraseña nueva.',
                  style: theme.textTheme.bodyMedium,
                ),
                const SizedBox(height: 20),
                Form(
                  key: _formEmail,
                  child: TextFormField(
                    controller: _email,
                    enabled: !_codigoEnviado && !_cargando,
                    keyboardType: TextInputType.emailAddress,
                    autocorrect: false,
                    decoration: const InputDecoration(
                      labelText: 'Email',
                      prefixIcon: Icon(Icons.email_outlined),
                    ),
                    validator: Validators.email,
                  ),
                ),
                const SizedBox(height: 16),
                if (!_codigoEnviado)
                  FilledButton(
                    onPressed: _cargando ? null : _enviarCodigo,
                    child: _cargando
                        ? const _Progreso()
                        : const Text('Enviar código'),
                  )
                else
                  Form(
                    key: _formCodigo,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        TextFormField(
                          controller: _codigo,
                          enabled: !_cargando,
                          keyboardType: TextInputType.number,
                          maxLength: 6,
                          inputFormatters: [
                            FilteringTextInputFormatter.digitsOnly,
                          ],
                          style: const TextStyle(
                            fontSize: 22,
                            letterSpacing: 8,
                          ),
                          decoration: const InputDecoration(
                            labelText: 'Código',
                            counterText: '',
                            prefixIcon: Icon(Icons.pin_outlined),
                          ),
                          validator: (v) => (v ?? '').length == 6
                              ? null
                              : 'Ingresá los 6 dígitos del código',
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _nueva,
                          enabled: !_cargando,
                          obscureText: _oculta,
                          decoration: InputDecoration(
                            labelText: 'Nueva contraseña',
                            prefixIcon: const Icon(Icons.lock_outline),
                            suffixIcon: IconButton(
                              tooltip: _oculta ? 'Mostrar' : 'Ocultar',
                              icon: Icon(
                                _oculta
                                    ? Icons.visibility_outlined
                                    : Icons.visibility_off_outlined,
                              ),
                              onPressed: () =>
                                  setState(() => _oculta = !_oculta),
                            ),
                          ),
                          validator: Validators.password,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _confirmacion,
                          enabled: !_cargando,
                          obscureText: _oculta,
                          decoration: const InputDecoration(
                            labelText: 'Repetí la nueva contraseña',
                            prefixIcon: Icon(Icons.lock_outline),
                          ),
                          validator: (v) => v == _nueva.text
                              ? null
                              : 'Las contraseñas no coinciden',
                        ),
                        const SizedBox(height: 20),
                        FilledButton(
                          onPressed: _cargando ? null : _restablecer,
                          child: _cargando
                              ? const _Progreso()
                              : const Text('Cambiar contraseña'),
                        ),
                        const SizedBox(height: 8),
                        TextButton(
                          onPressed: _cargando
                              ? null
                              : () => setState(() {
                                  _codigoEnviado = false;
                                  _codigo.clear();
                                }),
                          child: const Text('¿No te llegó? Pedir otro código'),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Progreso extends StatelessWidget {
  const _Progreso();

  @override
  Widget build(BuildContext context) {
    return const SizedBox(
      height: 20,
      width: 20,
      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
    );
  }
}
