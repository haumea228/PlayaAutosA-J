import '../../../core/network/dio_client.dart';
import 'models/cliente_model.dart';

// GET/PUT /api/Clientes/{id} son AdministradorP/Vendedor-only (y
// "/api/Clientes/perfil" no existe); el equivalente para un Cliente es
// MiCuentaController (/api/micuenta/perfil) — sin {id}, el backend identifica
// al cliente por el JWT.
class ProfileRepository {
  Future<ClienteModel> getPerfil() async {
    final response = await DioClient.get('MiCuenta/perfil');
    return ClienteModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Cambio de contraseña con la sesión iniciada: la API verifica la actual.
  Future<void> cambiarContrasena({
    required String actual,
    required String nueva,
  }) async {
    await DioClient.put(
      'MiCuenta/contrasena',
      data: {'passwordActual': actual, 'nuevaPassword': nueva},
    );
  }

  /// PUT /api/micuenta/perfil acepta nombre/telefono/direccion, el CI/RUC
  /// (SOLO si el cliente todavía no lo tenía: se carga una única vez) y el
  /// email. El email es también el usuario para iniciar sesión: para
  /// cambiarlo la API exige la contraseña actual ([passwordActual]) y que no
  /// esté usado por otra cuenta, y devuelve un token nuevo (el email viaja en
  /// el token), que se guarda acá para que la sesión siga al día.
  ///
  /// Devuelve true si el email cambió.
  Future<bool> actualizarPerfil(
    ClienteModel cliente, {
    String? passwordActual,
  }) async {
    final response = await DioClient.put(
      'MiCuenta/perfil',
      data: {
        'nombre': cliente.nombre,
        'telefono': cliente.telefono ?? '',
        'direccion': cliente.direccion ?? '',
        if (cliente.tieneCiRuc) 'cI_RUC': cliente.ciRuc,
        'email': ?cliente.email,
        'passwordActual': ?passwordActual,
      },
    );

    final body = response.data;
    final token = body is Map<String, dynamic>
        ? body['token'] as String?
        : null;
    if (token != null && token.isNotEmpty) {
      await DioClient.setToken(token);
      return true;
    }
    return false;
  }
}
