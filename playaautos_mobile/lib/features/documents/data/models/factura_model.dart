// Campos verificados contra FacturaDto.cs real (no contra el spec pegado,
// que tenía "ventaId" — ese campo no existe en el DTO). La propiedad C#
// "IVA" (todo mayúsculas) serializa como "iva" en el JSON real — verificado
// contra la respuesta real del servidor, no asumido por la convención
// camelCase habitual (un acrónimo completo en mayúsculas se baja entero).
//
// rutaPDF existe en el DTO pero el backend nunca lo completa (columna
// muerta) — se omite acá a propósito. El PDF real se pide aparte con
// GET /api/micuenta/facturas/{id}/pdf, que lo genera al vuelo.
class FacturaModel {
  const FacturaModel({
    required this.id,
    required this.numeroFactura,
    required this.timbrado,
    required this.fechaEmision,
    required this.cliente,
    required this.vehiculo,
    required this.subtotal,
    required this.iva,
    required this.total,
  });

  final int id;
  final String numeroFactura;
  final String timbrado;
  final DateTime fechaEmision;
  final String cliente;
  final String vehiculo;
  final int subtotal;
  final int iva;
  final int total;

  factory FacturaModel.fromJson(Map<String, dynamic> json) {
    return FacturaModel(
      id: json['facturaId'] as int,
      numeroFactura: json['numeroFactura'] as String? ?? '',
      timbrado: json['timbrado'] as String? ?? '',
      fechaEmision: DateTime.parse(json['fechaEmision'] as String),
      cliente: json['cliente'] as String? ?? '',
      vehiculo: json['vehiculo'] as String? ?? '',
      subtotal: json['subtotal'] as int? ?? 0,
      iva: json['iva'] as int? ?? 0,
      total: json['total'] as int? ?? 0,
    );
  }
}
