import 'dart:typed_data';

import '../../../core/network/dio_client.dart';
import '../../installments/data/models/cuota_model.dart';
import 'models/contrato_consigna_model.dart';
import 'models/factura_model.dart';
import 'models/nota_credito_model.dart';

// Facturas/NotasCredito/Contratos/Cuotas-ticket no tenían ninguna ruta
// accesible para el rol Cliente (todas eran AdministradorP/Vendedor/Cajero);
// se agregaron las de acá abajo en MiCuentaController siguiendo el mismo
// patrón que ya usa /api/micuenta/citas.
class DocumentsRepository {
  Future<List<FacturaModel>> getMisFacturas() async {
    final response = await DioClient.get('MiCuenta/facturas');
    return (response.data as List)
        .map((e) => FacturaModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<NotaCreditoModel>> getMisNotasCredito() async {
    final response = await DioClient.get('MiCuenta/notascredito');
    return (response.data as List)
        .map((e) => NotaCreditoModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<ContratoConsignaModel>> getMisContratos() async {
    final response = await DioClient.get('MiCuenta/contratos');
    return (response.data as List)
        .map((e) => ContratoConsignaModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Solo las cuotas ya pagadas tienen ticket (el backend devuelve 400 si se
  /// pide el ticket de una cuota pendiente), así que se filtran acá.
  Future<List<CuotaModel>> getMisCuotasPagadas() async {
    final response = await DioClient.get('MiCuenta/cuotas');
    return (response.data as List)
        .map((e) => CuotaModel.fromJson(e as Map<String, dynamic>))
        .where((cuota) => cuota.estaPagada)
        .toList();
  }

  Future<Uint8List> descargarFacturaPdf(int id) {
    return _descargarBytes('MiCuenta/facturas/$id/pdf');
  }

  Future<Uint8List> descargarNotaCreditoPdf(int id) {
    return _descargarBytes('MiCuenta/notascredito/$id/pdf');
  }

  Future<Uint8List> descargarTicketCuota(int id) {
    return _descargarBytes('MiCuenta/cuotas/$id/ticket');
  }

  Future<Uint8List> _descargarBytes(String path) async {
    final response = await DioClient.getBytes(path);
    return Uint8List.fromList(response.data ?? const []);
  }
}
