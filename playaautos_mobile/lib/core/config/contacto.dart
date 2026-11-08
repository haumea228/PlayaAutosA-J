import 'package:url_launcher/url_launcher.dart';

/// Datos de contacto de la playa (pantalla Perfil → "Acerca de nosotros" y
/// solicitudes de corrección de datos, como el CI/RUC).
abstract final class Contacto {
  static const String email = 'ayjautosusados@gmail.com';
  static const String telefono = '0986 908 266';

  /// Formato internacional (Paraguay +595, sin el 0 inicial) para wa.me.
  static const String whatsapp = '595986908266';

  /// Abre el correo del teléfono con destinatario, asunto y cuerpo cargados.
  static Future<bool> abrirEmail({required String asunto, String cuerpo = ''}) {
    // Uri(queryParameters:) codifica los espacios como "+", que algunas apps
    // de correo muestran literalmente; se arma la query a mano con %20.
    final query = 'subject=${Uri.encodeComponent(asunto)}'
        '${cuerpo.isEmpty ? '' : '&body=${Uri.encodeComponent(cuerpo)}'}';
    return launchUrl(Uri.parse('mailto:$email?$query'));
  }

  /// Abre directamente el chat de WhatsApp con la playa (app o navegador).
  static Future<bool> abrirWhatsApp({String mensaje = ''}) {
    final uri = Uri.parse(
      'https://wa.me/$whatsapp${mensaje.isEmpty ? '' : '?text=${Uri.encodeComponent(mensaje)}'}',
    );
    return launchUrl(uri, mode: LaunchMode.externalApplication);
  }
}
