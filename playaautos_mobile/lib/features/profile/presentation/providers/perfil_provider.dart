import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/models/cliente_model.dart';
import '../../data/profile_repository.dart';

final profileRepositoryProvider = Provider<ProfileRepository>((ref) {
  return ProfileRepository();
});

final perfilProvider = FutureProvider<ClienteModel>((ref) {
  return ref.watch(profileRepositoryProvider).getPerfil();
});

class ActualizarPerfilState {
  const ActualizarPerfilState({
    this.isLoading = false,
    this.error,
    this.success = false,
    this.emailCambiado = false,
  });

  final bool isLoading;
  final String? error;
  final bool success;

  /// El guardado cambió el email de la cuenta (el usuario para iniciar sesión).
  final bool emailCambiado;
}

class ActualizarPerfilNotifier extends StateNotifier<ActualizarPerfilState> {
  ActualizarPerfilNotifier(this._ref, this._repository)
    : super(const ActualizarPerfilState());

  final Ref _ref;
  final ProfileRepository _repository;

  Future<void> actualizar(
    ClienteModel cliente, {
    String? passwordActual,
  }) async {
    state = const ActualizarPerfilState(isLoading: true);
    try {
      final emailCambiado = await _repository.actualizarPerfil(
        cliente,
        passwordActual: passwordActual,
      );
      // perfilProvider vuelve a pedir /micuenta/perfil, así que el resto de
      // la app (ej. el prefill de agendar_cita_screen) ve los datos nuevos.
      _ref.invalidate(perfilProvider);
      state = ActualizarPerfilState(
        success: true,
        emailCambiado: emailCambiado,
      );
    } on DioException catch (e) {
      state = ActualizarPerfilState(
        error: e.message ?? 'No se pudo actualizar el perfil.',
      );
    }
  }
}

final actualizarPerfilProvider =
    StateNotifierProvider<ActualizarPerfilNotifier, ActualizarPerfilState>((
      ref,
    ) {
      return ActualizarPerfilNotifier(
        ref,
        ref.watch(profileRepositoryProvider),
      );
    });
