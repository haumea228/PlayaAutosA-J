import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/dio_client.dart';
import '../../../../core/services/notification_service.dart';
import '../../../appointments/presentation/providers/citas_provider.dart';
import '../../../documents/presentation/providers/documentos_provider.dart';
import '../../../installments/presentation/providers/cuotas_provider.dart';
import '../../../profile/presentation/providers/perfil_provider.dart';
import '../../data/auth_repository.dart';
import '../../data/models/user_model.dart';

sealed class AuthState {
  const AuthState();
}

class AuthLoading extends AuthState {
  const AuthLoading();
}

class AuthAuthenticated extends AuthState {
  const AuthAuthenticated(this.user);

  final UserModel user;
}

class AuthUnauthenticated extends AuthState {
  const AuthUnauthenticated();
}

class AuthError extends AuthState {
  const AuthError(this.message);

  final String message;
}

class AuthNotifier extends StateNotifier<AuthState> {
  AuthNotifier(this._ref, this._repository) : super(const AuthLoading()) {
    checkAuthStatus();
    // perfilProvider/misCitasProvider/los de Mis Archivos son FutureProvider
    // simples (no autoDispose): Riverpod cachea su resultado para toda la
    // vida de la app. Sin esto, cambiar de cuenta en la misma sesión de la
    // app (logout + login con otro usuario, o un login nuevo después de que
    // el token anterior expiró) seguía mostrando el perfil/documentos del
    // cliente anterior, porque nada los invalidaba. sessionNotifier ya
    // avisa de los tres casos (login, logout, 401 detectado en cualquier
    // request) así que alcanza con escucharlo acá una sola vez.
    DioClient.sessionNotifier.addListener(_invalidarDatosDelCliente);
  }

  final Ref _ref;
  final AuthRepository _repository;

  void _invalidarDatosDelCliente() {
    _ref.invalidate(perfilProvider);
    _ref.invalidate(misCitasProvider);
    _ref.invalidate(misFacturasProvider);
    _ref.invalidate(misNotasCreditoProvider);
    _ref.invalidate(misContratosProvider);
    _ref.invalidate(misTicketsCuotaProvider);
    _ref.invalidate(misCuotasProvider);
  }

  @override
  void dispose() {
    DioClient.sessionNotifier.removeListener(_invalidarDatosDelCliente);
    super.dispose();
  }

  Future<void> checkAuthStatus() async {
    state = const AuthLoading();
    final user = await _repository.getCurrentUser();
    state = user != null
        ? AuthAuthenticated(user)
        : const AuthUnauthenticated();
  }

  Future<void> login(String email, String password) async {
    state = const AuthLoading();
    try {
      final user = await _repository.login(email, password);
      state = AuthAuthenticated(user);
      unawaited(NotificationService.instance.registrarDispositivo());
    } on DioException catch (e) {
      state = AuthError(e.message ?? 'No se pudo iniciar sesión.');
    }
  }

  Future<void> register(Map<String, dynamic> data) async {
    state = const AuthLoading();
    try {
      final user = await _repository.register(data);
      state = AuthAuthenticated(user);
      unawaited(NotificationService.instance.registrarDispositivo());
    } on DioException catch (e) {
      state = AuthError(e.message ?? 'No se pudo crear la cuenta.');
    }
  }

  Future<void> logout() async {
    await _repository.logout();
    state = const AuthUnauthenticated();
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref, AuthRepository());
});
