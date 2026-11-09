/// Validadores de formularios compartidos.
abstract final class Validators {
  static final RegExp _ciRuc = RegExp(r'^\d{5,8}(-\d)?$');
  static final RegExp _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  /// Contraseña nueva: mismo mínimo que la API (6 caracteres).
  static String? password(String? valor) {
    final v = valor ?? '';
    if (v.length < 6) return 'Usá al menos 6 caracteres';
    if (v.length > 100) return 'La contraseña es demasiado larga';
    return null;
  }

  /// Email obligatorio con formato básico (la API vuelve a validarlo).
  static String? email(String? valor) {
    final limpio = (valor ?? '').trim();
    if (limpio.isEmpty) return 'Ingresá tu email';
    if (limpio.length > 150) return 'El email es demasiado largo';
    return _email.hasMatch(limpio) ? null : 'Ingresá un email válido';
  }

  /// Quita puntos y espacios (mismo criterio que la API: Helpers/CiRuc.cs).
  static String normalizarCiRuc(String valor) =>
      valor.replaceAll('.', '').replaceAll(' ', '').trim();

  /// CI de 5 a 8 números, o RUC con dígito verificador ("80012345-6").
  /// Vacío es válido: el CI/RUC es opcional hasta que el cliente lo carga.
  static String? ciRuc(String? valor) {
    final limpio = normalizarCiRuc(valor ?? '');
    if (limpio.isEmpty) return null;
    return _ciRuc.hasMatch(limpio)
        ? null
        : 'Ingresá entre 5 y 8 números, sin puntos (RUC: 80012345-6)';
  }
}
