import 'package:intl/intl.dart';

/// Formatos compartidos (montos en guaraníes y fechas).
abstract final class Formatters {
  static final NumberFormat _miles = NumberFormat.decimalPattern('es');
  static final DateFormat _fecha = DateFormat('dd/MM/yyyy');
  static final DateFormat _fechaHora = DateFormat('dd/MM/yyyy HH:mm');

  /// 1500000 -> "₲ 1.500.000"
  static String guaranies(num monto) => '₲ ${_miles.format(monto)}';

  /// 120000 -> "120.000"
  static String numero(num valor) => _miles.format(valor);

  static String fecha(DateTime fecha) => _fecha.format(fecha);

  static String fechaHora(DateTime fecha) => _fechaHora.format(fecha);
}
