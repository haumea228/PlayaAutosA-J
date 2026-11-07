import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../documents/presentation/providers/documentos_provider.dart';
import '../../data/installments_repository.dart';
import '../../data/models/cuota_model.dart';

final installmentsRepositoryProvider = Provider<InstallmentsRepository>((ref) {
  return InstallmentsRepository();
});

final misCuotasProvider = FutureProvider<List<CuotaModel>>((ref) {
  return ref.watch(installmentsRepositoryProvider).getMisCuotas();
});

class ReportarPagoState {
  const ReportarPagoState({this.isLoading = false, this.error, this.mensaje});

  final bool isLoading;
  final String? error;

  /// Mensaje de confirmación del backend; no nulo = enviado con éxito.
  final String? mensaje;
}

class ReportarPagoNotifier extends StateNotifier<ReportarPagoState> {
  ReportarPagoNotifier(this._ref, this._repository)
      : super(const ReportarPagoState());

  final Ref _ref;
  final InstallmentsRepository _repository;

  Future<void> enviar({
    required int cuotaId,
    required File comprobante,
    String? observacion,
  }) async {
    state = const ReportarPagoState(isLoading: true);
    try {
      final mensaje = await _repository.reportarPago(
        cuotaId: cuotaId,
        comprobante: comprobante,
        observacion: observacion,
      );
      // La cuota pasó a "En revisión": refrescar la lista y los tickets.
      _ref.invalidate(misCuotasProvider);
      _ref.invalidate(misTicketsCuotaProvider);
      state = ReportarPagoState(mensaje: mensaje);
    } on DioException catch (e) {
      state = ReportarPagoState(
        error: e.message ?? 'No se pudo enviar el comprobante.',
      );
    }
  }
}

final reportarPagoProvider = StateNotifierProvider.autoDispose<
    ReportarPagoNotifier, ReportarPagoState>((ref) {
  return ReportarPagoNotifier(ref, ref.watch(installmentsRepositoryProvider));
});
