import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../installments/data/models/cuota_model.dart';
import '../../data/documents_repository.dart';
import '../../data/models/contrato_consigna_model.dart';
import '../../data/models/factura_model.dart';
import '../../data/models/nota_credito_model.dart';

final documentsRepositoryProvider = Provider<DocumentsRepository>((ref) {
  return DocumentsRepository();
});

final misFacturasProvider = FutureProvider<List<FacturaModel>>((ref) {
  return ref.watch(documentsRepositoryProvider).getMisFacturas();
});

final misNotasCreditoProvider = FutureProvider<List<NotaCreditoModel>>((ref) {
  return ref.watch(documentsRepositoryProvider).getMisNotasCredito();
});

final misContratosProvider = FutureProvider<List<ContratoConsignaModel>>((
  ref,
) {
  return ref.watch(documentsRepositoryProvider).getMisContratos();
});

final misTicketsCuotaProvider = FutureProvider<List<CuotaModel>>((ref) {
  return ref.watch(documentsRepositoryProvider).getMisCuotasPagadas();
});
