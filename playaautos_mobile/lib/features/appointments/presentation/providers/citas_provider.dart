import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/appointments_repository.dart';
import '../../data/models/cita_model.dart';

final appointmentsRepositoryProvider = Provider<AppointmentsRepository>((ref) {
  return AppointmentsRepository();
});

final misCitasProvider = FutureProvider<List<CitaModel>>((ref) {
  return ref.watch(appointmentsRepositoryProvider).getMisCitas();
});

final horariosDisponiblesProvider =
    FutureProvider.family<List<String>, DateTime>((ref, fecha) {
  return ref
      .watch(appointmentsRepositoryProvider)
      .getHorariosDisponibles(fecha);
});

class AgendarCitaState {
  const AgendarCitaState({this.isLoading = false, this.error, this.citaId});

  final bool isLoading;
  final String? error;
  final int? citaId;
}

class AgendarCitaNotifier extends StateNotifier<AgendarCitaState> {
  AgendarCitaNotifier(this._repository) : super(const AgendarCitaState());

  final AppointmentsRepository _repository;

  Future<void> agendar({
    int? vehiculoId,
    required DateTime fechaHora,
    String tipoCita = 'Consulta',
    String? observaciones,
  }) async {
    state = const AgendarCitaState(isLoading: true);
    try {
      final citaId = await _repository.agendarCita(
        vehiculoId: vehiculoId,
        fechaHora: fechaHora,
        tipoCita: tipoCita,
        observaciones: observaciones,
      );
      state = AgendarCitaState(citaId: citaId);
    } on DioException catch (e) {
      state = AgendarCitaState(error: e.message ?? 'No se pudo agendar la cita.');
    }
  }

  void reset() => state = const AgendarCitaState();
}

final agendarCitaProvider =
    StateNotifierProvider<AgendarCitaNotifier, AgendarCitaState>((ref) {
  return AgendarCitaNotifier(ref.watch(appointmentsRepositoryProvider));
});
