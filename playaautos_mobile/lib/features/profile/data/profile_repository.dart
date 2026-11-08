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

  /// PUT /api/micuenta/perfil acepta nombre/telefono/direccion y el CI/RUC,
  /// este último SOLO si el cliente todavía no lo tenía (se carga una única
  /// vez; si ya existe, la API rechaza cualquier cambio). Email y rangoPrecio
  /// no son editables por el cliente.
  Future<void> actualizarPerfil(ClienteModel cliente) async {
    await DioClient.put(
      'MiCuenta/perfil',
      data: {
        'nombre': cliente.nombre,
        'telefono': cliente.telefono ?? '',
        'direccion': cliente.direccion ?? '',
        if (cliente.tieneCiRuc) 'cI_RUC': cliente.ciRuc,
      },
    );
  }
}
