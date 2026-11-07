import '../../../core/network/dio_client.dart';
import 'models/mensaje_asistente.dart';

/// POST /api/asistente/consultar (público, como el catálogo). La API llama al
/// modelo de IA y valida que los vehículos recomendados existan; si la IA no
/// está disponible responde con un filtro de reglas propio.
class AsistenteRepository {
  /// Cantidad de mensajes previos que se envían como contexto.
  static const int _maxHistorial = 6;

  Future<MensajeAsistente> consultar(
    String pregunta,
    List<MensajeAsistente> conversacion,
  ) async {
    final previos = conversacion.where((m) => !m.esError).toList();
    final historial = previos
        .skip(previos.length > _maxHistorial ? previos.length - _maxHistorial : 0)
        .map((m) => m.toHistorial())
        .toList();

    final response = await DioClient.post(
      'Asistente/consultar',
      data: {'pregunta': pregunta, 'historial': historial},
    );

    final body = response.data as Map<String, dynamic>;
    return MensajeAsistente(
      esUsuario: false,
      texto: body['mensaje'] as String? ?? '',
      vehiculos: (body['vehiculos'] as List<dynamic>? ?? [])
          .map((e) => VehiculoRecomendado.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
