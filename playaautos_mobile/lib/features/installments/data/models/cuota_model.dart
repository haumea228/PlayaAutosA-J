// Campos tomados de MiCuotaDto (GET /api/micuenta/cuotas).
class CuotaModel {
  const CuotaModel({
    required this.id,
    required this.ventaId,
    required this.numeroCuota,
    this.cantidadCuotas,
    required this.monto,
    required this.fechaVencimiento,
    this.fechaPago,
    this.montoPagado,
    this.montoRecargo,
    required this.estado,
    required this.cliente,
    required this.vehiculo,
    this.recargoCalculado = 0,
    int? montoAPagar,
    this.puedeInformarPago = false,
    this.motivoNoPuedePagar,
    this.pagoConTarjetaDisponible = false,
    this.ultimaSolicitudEstado,
    this.ultimaSolicitudFecha,
    this.ultimaSolicitudMotivoRechazo,
  }) : montoAPagar = montoAPagar ?? monto;

  final int id;
  final int ventaId;
  final int numeroCuota;
  final int? cantidadCuotas;
  final int monto;
  final DateTime fechaVencimiento;
  final DateTime? fechaPago;
  final int? montoPagado;
  final int? montoRecargo;

  /// Pendiente | EnRevision | Pagada | Anulada
  final String estado;
  final String cliente;
  final String vehiculo;

  /// Recargo por mora si el pago se informara hoy (0 si no está vencida).
  final int recargoCalculado;

  /// Monto + recargo: lo que el cliente tiene que transferir hoy.
  final int montoAPagar;

  /// Solo la cuota pendiente más antigua de cada venta se puede pagar.
  final bool puedeInformarPago;
  final String? motivoNoPuedePagar;

  /// La pasarela de pago con tarjeta (Stripe) está configurada en la API.
  final bool pagoConTarjetaDisponible;

  final String? ultimaSolicitudEstado;
  final DateTime? ultimaSolicitudFecha;
  final String? ultimaSolicitudMotivoRechazo;

  bool get estaPagada => estado == 'Pagada';
  bool get estaEnRevision => estado == 'EnRevision';
  bool get estaPendiente => estado == 'Pendiente';
  bool get estaVencida =>
      estaPendiente && fechaVencimiento.isBefore(DateTime.now());

  /// El último comprobante enviado fue rechazado y la cuota sigue impaga.
  bool get fueRechazada =>
      estaPendiente && ultimaSolicitudEstado == 'Rechazada';

  factory CuotaModel.fromJson(Map<String, dynamic> json) {
    DateTime? fecha(String key) =>
        json[key] != null ? DateTime.parse(json[key] as String) : null;

    final monto = (json['monto'] as num?)?.toInt() ?? 0;

    return CuotaModel(
      id: json['cuotaId'] as int,
      ventaId: json['ventaId'] as int,
      numeroCuota: json['numeroCuota'] as int,
      cantidadCuotas: json['cantidadCuotas'] as int?,
      monto: monto,
      fechaVencimiento: DateTime.parse(json['fechaVencimiento'] as String),
      fechaPago: fecha('fechaPago'),
      montoPagado: (json['montoPagado'] as num?)?.toInt(),
      montoRecargo: (json['montoRecargo'] as num?)?.toInt(),
      estado: json['estado'] as String? ?? '',
      cliente: json['cliente'] as String? ?? '',
      vehiculo: json['vehiculo'] as String? ?? '',
      recargoCalculado: (json['recargoCalculado'] as num?)?.toInt() ?? 0,
      montoAPagar: (json['montoAPagar'] as num?)?.toInt() ?? monto,
      puedeInformarPago: json['puedeInformarPago'] as bool? ?? false,
      motivoNoPuedePagar: json['motivoNoPuedePagar'] as String?,
      pagoConTarjetaDisponible:
          json['pagoConTarjetaDisponible'] as bool? ?? false,
      ultimaSolicitudEstado: json['ultimaSolicitudEstado'] as String?,
      ultimaSolicitudFecha: fecha('ultimaSolicitudFecha'),
      ultimaSolicitudMotivoRechazo:
          json['ultimaSolicitudMotivoRechazo'] as String?,
    );
  }
}
