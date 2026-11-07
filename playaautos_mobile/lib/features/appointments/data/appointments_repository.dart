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
  /// AgendarCitaClienteDto (el DTO real de POST /api/micuenta/citas/agendar)
  /// exige nombre/telefono/direccion — el backend los usa para actualizar el
  /// perfil del cliente si cambiaron, igual que hacía la web: la primera vez
  /// se completan y quedan guardados; de ahí en más son editables (excepto
  /// el CI/RUC, que el backend solo graba si el cliente todavía no tiene
  /// uno — por eso ciRuc es opcional acá, la pantalla decide si corresponde
  /// enviarlo). Por eso estos datos los pasa quien llama (la pantalla, ya
  /// con el perfil precargado) en vez de que el repository los adivine.
  Future<int> agendarCita({
    int? vehiculoId,
    required DateTime fechaHora,
    required String nombre,
    required String telefono,
    required String direccion,
    String? ciRuc,
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
        'nombre': nombre,
        'telefono': telefono,
        'direccion': direccion,
        if (ciRuc != null && ciRuc.isNotEmpty) 'cI_RUC': ciRuc,
      },
    );

    final body = response.data as Map<String, dynamic>;
    return body['citaId'] as int;
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

    return [
      for (var hora = 8; hora <= 18; hora++)
        '${hora.toString().padLeft(2, '0')}:00',
    ];
  }
}
