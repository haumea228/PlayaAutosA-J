import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../config/api_config.dart';

/// Notifica a quien escuche (el router, vía `refreshListenable`) cada vez que
/// cambia el estado de sesión — login, logout o un 401 detectado en
/// cualquier request. Así el redirect de GoRouter se re-evalúa al toque en
/// vez de esperar a que el usuario navegue manualmente.
class SessionNotifier extends ChangeNotifier {
  void notify() => notifyListeners();
}

/// Shared Dio-based HTTP client: injects the JWT token on every request
/// and normalizes DioException messages into user-facing Spanish text.
class DioClient {
  DioClient._();

  static const FlutterSecureStorage _storage = FlutterSecureStorage();
  static const String _tokenKey = 'jwt_token';

  static final SessionNotifier sessionNotifier = SessionNotifier();

  static final Dio _dio = Dio(
    BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: ApiConfig.connectTimeout,
      receiveTimeout: ApiConfig.receiveTimeout,
      headers: ApiConfig.defaultHeaders,
    ),
  )..interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _storage.read(key: _tokenKey);
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          // 401 = token ausente/inválido/vencido en un endpoint protegido.
          // Se borra acá mismo y se avisa al router para que mande a /login
          // en el momento, en vez de dejar al usuario en una pantalla de
          // error sin salida (lo que pasaba antes: cada pantalla mostraba
          // "Sesión expirada" con un botón "Reintentar" que repetía el mismo
          // request con el mismo token vencido, en loop).
          if (error.response?.statusCode == 401) {
            await clearToken();
          }
          handler.next(_mapError(error));
        },
      ),
    );

  static Future<String?> getToken() {
    return _storage.read(key: _tokenKey);
  }

  static Future<void> setToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
    sessionNotifier.notify();
  }

  static Future<void> clearToken() async {
    await _storage.delete(key: _tokenKey);
    sessionNotifier.notify();
  }

  static Future<Response<T>> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) {
    return _dio.get<T>(path, queryParameters: queryParameters);
  }

  static Future<Response<T>> post<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) {
    return _dio.post<T>(path, data: data, queryParameters: queryParameters);
  }

  static Future<Response<T>> put<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) {
    return _dio.put<T>(path, data: data, queryParameters: queryParameters);
  }

  static Future<Response<T>> delete<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) {
    return _dio.delete<T>(path, data: data, queryParameters: queryParameters);
  }

  /// Para endpoints que devuelven un archivo binario (ej. los PDF de
  /// factura/nota de crédito/ticket, generados al vuelo por el backend y
  /// devueltos como `application/pdf`, no como una ruta estática).
  static Future<Response<List<int>>> getBytes(String path) {
    return _dio.get<List<int>>(
      path,
      options: Options(responseType: ResponseType.bytes),
    );
  }

  static DioException _mapError(DioException error) {
    // Prefer the API's own message (e.g. "Email o contraseña incorrectos.")
    // over a generic one keyed only by HTTP status, since the same status
    // code (401, 400) means different things on different endpoints.
    final message = _extractServerMessage(error) ?? _fallbackMessage(error);
    return error.copyWith(message: message);
  }

  static String? _extractServerMessage(DioException error) {
    final data = error.response?.data;
    if (data is String && data.trim().isNotEmpty) {
      return data;
    }
    if (data is Map<String, dynamic>) {
      final value = data['message'] ?? data['title'] ?? data['error'];
      if (value is String && value.trim().isNotEmpty) {
        return value;
      }
    }
    return null;
  }

  static String _fallbackMessage(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.transformTimeout:
        return 'Tiempo de espera agotado. Verifica tu conexión.';
      case DioExceptionType.connectionError:
        return 'No se pudo conectar con el servidor.';
      case DioExceptionType.cancel:
        return 'Solicitud cancelada.';
      case DioExceptionType.badResponse:
        return _messageForStatusCode(error.response?.statusCode);
      case DioExceptionType.badCertificate:
        return 'Certificado del servidor no válido.';
      case DioExceptionType.unknown:
        return 'Ocurrió un error inesperado.';
    }
  }

  static String _messageForStatusCode(int? statusCode) {
    switch (statusCode) {
      case 400:
        return 'Solicitud inválida.';
      case 401:
        return 'Sesión expirada. Inicia sesión nuevamente.';
      case 403:
        return 'No tienes permisos para realizar esta acción.';
      case 404:
        return 'Recurso no encontrado.';
      case 500:
        return 'Error interno del servidor.';
      default:
        return 'Error de conexión con el servidor.';
    }
  }
}
