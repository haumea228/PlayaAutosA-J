import 'dart:convert';

import '../../../core/network/dio_client.dart';
import '../../../core/services/notification_service.dart';
import 'models/user_model.dart';

// Claim keys as actually written by JwtHelper.GenerarToken on the API side
// (System.Security.Claims.ClaimTypes.* serialize to these long URIs, not to
// short names like "sub"/"email" — verified against a real token).
const String _claimId =
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const String _claimEmail =
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
const String _claimNombre =
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const String _claimRol =
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

class AuthRepository {
  Future<UserModel> login(String email, String password) async {
    final response = await DioClient.post(
      'Auth/login',
      data: {'email': email, 'password': password},
    );
    final body = response.data as Map<String, dynamic>;

    // LoginResponseDto no incluye el email, así que se usa el que el
    // cliente acaba de escribir para iniciar sesión.
    final user = UserModel(
      id: body['usuarioId'] as int,
      email: email,
      nombre: body['nombre'] as String,
      rol: body['rol'] as String,
      token: body['token'] as String,
    );

    await DioClient.setToken(user.token!);
    return user;
  }

  Future<UserModel> register(Map<String, dynamic> data) async {
    // /api/Auth/registro crea un usuario AdministradorP: NO usar desde la
    // app de clientes. El endpoint correcto es registro-cliente, que solo
    // confirma la creación de la cuenta (no devuelve token), por lo que se
    // encadena un login para dejar al cliente con sesión iniciada.
    await DioClient.post('Auth/registro-cliente', data: data);
    return login(data['email'] as String, data['password'] as String);
  }

  Future<void> logout() async {
    // Primero la baja del dispositivo (requiere el JWT todavía válido), para
    // que este teléfono deje de recibir avisos de esta cuenta.
    await NotificationService.instance.desregistrarDispositivo();
    await DioClient.clearToken();
  }

  Future<String?> getToken() {
    return DioClient.getToken();
  }

  Future<bool> isLoggedIn() async {
    return (await DioClient.getToken()) != null;
  }

  /// Reconstruye el UserModel a partir de los claims del JWT guardado, sin
  /// llamar a la API (no existe un endpoint "/me"). Si el token expiró, lo
  /// borra y devuelve null para no dejar la app en un estado "autenticado"
  /// donde cada request fallaría con 401.
  Future<UserModel?> getCurrentUser() async {
    final token = await DioClient.getToken();
    if (token == null) return null;

    final Map<String, dynamic> claims;
    try {
      claims = _decodeJwtPayload(token);
    } on FormatException {
      await DioClient.clearToken();
      return null;
    }

    final exp = claims['exp'] as int?;
    if (exp != null &&
        DateTime.now().isAfter(
          DateTime.fromMillisecondsSinceEpoch(exp * 1000),
        )) {
      await DioClient.clearToken();
      return null;
    }

    final id = int.tryParse(claims[_claimId] as String? ?? '');
    final email = claims[_claimEmail] as String?;
    final nombre = claims[_claimNombre] as String?;
    final rol = claims[_claimRol] as String?;

    if (id == null || email == null || nombre == null || rol == null) {
      return null;
    }

    return UserModel(id: id, email: email, nombre: nombre, rol: rol, token: token);
  }

  Map<String, dynamic> _decodeJwtPayload(String token) {
    final segments = token.split('.');
    final normalized = base64Url.normalize(segments[1]);
    return jsonDecode(utf8.decode(base64Url.decode(normalized)))
        as Map<String, dynamic>;
  }
}
