import 'dart:io';

import 'package:dio/dio.dart';

import '../../../core/network/dio_client.dart';
import 'models/cuota_model.dart';
import 'models/pago_online_model.dart';

// /api/Cuotas es solo para el personal (y su PUT /pagar registra el cobro en
// caja directamente). El cliente usa MiCuentaController: lista sus cuotas e
// INFORMA un pago, que queda "EnRevision" hasta que Caja lo apruebe desde la
// web; recién ahí la cuota pasa a "Pagada" y se habilita el ticket.
class InstallmentsRepository {
  Future<List<CuotaModel>> getMisCuotas() async {
    final response = await DioClient.get('MiCuenta/cuotas');
    return (response.data as List)
        .map((e) => CuotaModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Envía la foto del comprobante de transferencia (multipart/form-data).
  Future<String> reportarPago({
    required int cuotaId,
    required File comprobante,
    String? observacion,
  }) async {
    final nombre = comprobante.uri.pathSegments.isNotEmpty
        ? comprobante.uri.pathSegments.last
        : 'comprobante.jpg';

    final form = FormData.fromMap({
      'comprobante': await MultipartFile.fromFile(
        comprobante.path,
        filename: nombre,
      ),
      if (observacion != null && observacion.trim().isNotEmpty)
        'observacion': observacion.trim(),
    });

    final response = await DioClient.post(
      'MiCuenta/cuotas/$cuotaId/reportar-pago',
      data: form,
    );

    final body = response.data;
    if (body is Map<String, dynamic> && body['mensaje'] is String) {
      return body['mensaje'] as String;
    }
    return 'Recibimos tu comprobante.';
  }

  /// Crea la página de pago con tarjeta (Stripe Checkout) de la cuota.
  Future<PagoOnlineModel> iniciarPagoConTarjeta(int cuotaId) async {
    final response = await DioClient.post('MiCuenta/cuotas/$cuotaId/pago-online');
    return PagoOnlineModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// La API consulta a Stripe si el pago se completó y, en ese caso, registra
  /// el pago de la cuota (caja + ticket).
  Future<PagoOnlineModel> verificarPagoConTarjeta(int pagoId) async {
    final response = await DioClient.post('MiCuenta/pagos-online/$pagoId/verificar');
    return PagoOnlineModel.fromJson(response.data as Map<String, dynamic>);
  }
}
