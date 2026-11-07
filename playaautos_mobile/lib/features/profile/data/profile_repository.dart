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

  /// ActualizarPerfilClienteDto (el DTO real de PUT /api/micuenta/perfil)
  /// solo acepta nombre/telefono/direccion — ni CI/RUC, ni email, ni
  /// rangoPrecio son editables por el cliente, así que esos campos de
  /// [cliente] se ignoran acá (ver nota en cliente_model.dart).
  Future<void> actualizarPerfil(ClienteModel cliente) async {
    await DioClient.put(
      'MiCuenta/perfil',
      data: {
        'nombre': cliente.nombre,
        'telefono': cliente.telefono ?? '',
        'direccion': cliente.direccion ?? '',
      },
    );
  }
}
