/// Validadores de formularios compartidos.
abstract final class Validators {
  static final RegExp _ciRuc = RegExp(r'^\d{5,8}(-\d)?$');

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
