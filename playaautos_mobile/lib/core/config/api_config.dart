class ApiConfig {
  // Emulador Android -> host: 10.0.2.2 apunta al localhost de la PC.
  // Usamos el puerto HTTP (5035) porque UseHttpsRedirection está
  // desactivado en Development para evitar el certificado autofirmado.
  static const String baseUrl = 'http://10.0.2.2:5035/api/';

  // Las fotos de vehículos vienen como rutas relativas (ej.
  // "/uploads/vehiculos/x.jpg"), servidas como archivos estáticos fuera de
  // /api/. mediaBaseUrl es el host sin ese prefijo, para armar la URL
  // completa: '$mediaBaseUrl${foto.url}'.
  static const String mediaBaseUrl = 'http://10.0.2.2:5035';

  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);

  static const Map<String, String> defaultHeaders = {
    'Content-Type': 'application/json',
  };
}
