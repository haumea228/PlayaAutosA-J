import '../../../core/network/dio_client.dart';
import 'models/cita_model.dart';

// Endpoints reales: GET/POST /api/Citas son AdministradorP/Vendedor-only
// (verificado en CitasController.cs); el equivalente para un Cliente es
// MiCuentaController (/api/micuenta/citas), que además solo devuelve/permite
// agendar las citas del cliente autenticado.
class AppointmentsRepository {
  Future<List<CitaModel>> getMisCitas() async {
    final response = await DioClient.get('MiCuenta/citas');
    final list = response.data as List<dynamic>;
    return list
        .map((e) => CitaModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// vehiculoId es opcional: el cliente puede agendar desde el detalle de un
  /// vehículo puntual, o sin asociar ninguno.
  ///
  /// Los datos de contacto (nombre, teléfono, dirección, CI/RUC) ya no se
  /// envían: se editan solo desde Perfil y la API usa los del perfil del
  /// cliente autenticado (exige que tenga nombre y teléfono).
  Future<int> agendarCita({
    int? vehiculoId,
    required DateTime fechaHora,
    String tipoCita = 'Consulta',
    String? observaciones,
  }) async {
    final response = await DioClient.post(
      'MiCuenta/citas/agendar',
      data: {
        'vehiculoId': vehiculoId,
        'fechaHora': fechaHora.toIso8601String(),
        'tipoCita': tipoCita,
        'observaciones': observaciones,
      },
    );

    final body = response.data as Map<String, dynamic>;
    return body['citaId'] as int;
  }

  /// Cancela una cita propia (Pendiente o Confirmada y todavía no pasada).
  /// El motivo es opcional y queda en la descripción para el encargado.
  Future<void> cancelarCita(int citaId, {String? motivo}) async {
    await DioClient.put(
      'MiCuenta/citas/$citaId/cancelar',
      data: {'motivo': motivo},
    );
  }

  /// No existe un endpoint de disponibilidad en el backend (ni tampoco
  /// verificación de choque entre citas en ningún lado: CitasController solo
  /// valida día/horario permitido al agendar). Se generan los horarios
  /// válidos client-side con esas mismas reglas — Lunes a Sábado, 08:00 a
  /// 18:00 — en vez de llamar a un endpoint que no existe. No excluye
  /// feriados (queda a cargo del selector de fecha) ni horarios ya tomados
  /// por otra cita, porque el sistema tampoco lo hace hoy.
  Future<List<String>> getHorariosDisponibles(DateTime fecha) async {
    if (fecha.weekday == DateTime.sunday) return [];

    // Si la cita es para hoy, no se ofrecen horarios que ya pasaron (la API
    // rechaza fechas en el pasado): solo desde la próxima hora en adelante.
    final ahora = DateTime.now();
    final esHoy = fecha.year == ahora.year &&
        fecha.month == ahora.month &&
        fecha.day == ahora.day;
    final desde = esHoy ? ahora.hour + 1 : 8;

    return [
      for (var hora = desde < 8 ? 8 : desde; hora <= 18; hora++)
        '${hora.toString().padLeft(2, '0')}:00',
    ];
  }
}
