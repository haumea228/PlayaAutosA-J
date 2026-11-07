// Campos tomados de NotaCreditoDto.cs real. A diferencia de Factura, esta
// DTO ni siquiera expone un campo rutaPDF (solo existe, sin usarse, en la
// entidad) — el PDF se pide aparte con
// GET /api/micuenta/notascredito/{id}/pdf.
//
// Se incluyen solo los campos que la pantalla de Mis Archivos realmente
// muestra; la DTO real trae varios más (clienteRUC, timbrado, vigencias,
// etc.) que no hacen falta para esta lista.
class NotaCreditoModel {
  const NotaCreditoModel({
    required this.id,
    required this.numeroNota,
    required this.numeroFactura,
    required this.fechaEmision,
    required this.vehiculoVenta,
    required this.motivo,
    required this.monto,
  });

  final int id;
  final String numeroNota;
  final String numeroFactura;
  final DateTime fechaEmision;
  final String vehiculoVenta;
  final String motivo;
  final int monto;

  factory NotaCreditoModel.fromJson(Map<String, dynamic> json) {
    return NotaCreditoModel(
      id: json['notaCreditoId'] as int,
      numeroNota: json['numeroNota'] as String? ?? '',
      numeroFactura: json['numeroFactura'] as String? ?? '',
      fechaEmision: DateTime.parse(json['fechaEmision'] as String),
      vehiculoVenta: json['vehiculoVenta'] as String? ?? '',
      motivo: json['motivo'] as String? ?? '',
      monto: json['monto'] as int? ?? 0,
    );
  }
}
