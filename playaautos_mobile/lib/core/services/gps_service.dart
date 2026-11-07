import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher.dart';

/// Ubicación de la agencia (A&J Autos Usados, Carapeguá, Paraguay).
///
/// Calculadas decodificando a mano el Plus Code del enlace de Maps que pasó
/// el cliente (https://maps.app.goo.gl/fv3mwJgPMDN7jyaX6 → "6QM4+Q47 Autos
/// importados, Tte. 1º Alcides González, Carapeguá 090502"), ya que ese
/// enlace corto no expone lat/lng directamente y solo se pudo resolver hasta
/// la página de Maps (JS, sin datos estáticos). El margen de error de una
/// decodificación manual de Plus Code es de pocos metros como mucho, pero
/// vale la pena confirmarlas abriendo "Ver ubicación de la agencia" una vez
/// y, si el pin no cae exactamente sobre el local, avisame para ajustarlas.
const double agenciaLat = -25.765586;
const double agenciaLng = -57.244800;
const String agenciaNombre = 'A&J Autos Usados';

class GpsService {
  final Geocoding _geocoding = Geocoding();

  Future<Position> getCurrentPosition() async {
    final servicioActivo = await Geolocator.isLocationServiceEnabled();
    if (!servicioActivo) {
      throw Exception('Activá el GPS para compartir tu ubicación.');
    }

    var permiso = await Geolocator.checkPermission();
    if (permiso == LocationPermission.denied) {
      permiso = await Geolocator.requestPermission();
    }
    if (permiso == LocationPermission.denied ||
        permiso == LocationPermission.deniedForever) {
      throw Exception('Permiso de ubicación denegado.');
    }

    return Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(accuracy: LocationAccuracy.high),
    );
  }

  Future<String> getAddressFromCoordinates(double lat, double lng) async {
    final lugares = await _geocoding.placemarkFromCoordinates(lat, lng);
    if (lugares.isEmpty) return '';

    final lugar = lugares.first;
    return [
      lugar.street,
      lugar.locality,
      lugar.administrativeArea,
      lugar.country,
    ].where((parte) => parte != null && parte.trim().isNotEmpty).join(', ');
  }

  Future<void> openMapsToAgency() async {
    final uri = Uri.parse(
      'https://www.google.com/maps/search/?api=1&query=$agenciaLat,$agenciaLng',
    );
    final abierto = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!abierto) {
      throw Exception('No se pudo abrir Maps.');
    }
  }
}
