/// Pago con tarjeta vía la pasarela (Stripe Checkout). Campos de PagoOnlineDto.
class PagoOnlineModel {
  const PagoOnlineModel({
    required this.id,
    required this.cuotaId,
    required this.total,
    required this.estado,
    this.url,
  });

  final int id;
  final int cuotaId;
  final int total;

  /// Pendiente | Pagado | Expirado | RequiereReembolso
  final String estado;

  /// Página de pago de Stripe (solo mientras está pendiente).
  final String? url;

  bool get estaPagado => estado == 'Pagado';
  bool get estaPendiente => estado == 'Pendiente';
  bool get requiereReembolso => estado == 'RequiereReembolso';

  factory PagoOnlineModel.fromJson(Map<String, dynamic> json) {
    return PagoOnlineModel(
      id: json['pagoOnlineId'] as int,
      cuotaId: json['cuotaId'] as int,
      total: (json['total'] as num).toInt(),
      estado: json['estado'] as String? ?? '',
      url: json['url'] as String?,
    );
  }
}
