import 'dart:io';

import 'package:device_info_plus/device_info_plus.dart';
import 'package:image_picker/image_picker.dart';
import 'package:permission_handler/permission_handler.dart';

/// Toma/selecciona la foto del comprobante de pago de una cuota.
class CameraService {
  final ImagePicker _picker = ImagePicker();

  // Suficiente para leer un comprobante bancario con nitidez, sin subir fotos
  // de 10+ MB (la API acepta hasta 8 MB).
  static const double _maxLado = 2400;

  Future<File?> pickImageFromCamera() async {
    if (!await _ensurePermission(Permission.camera)) return null;
    final foto = await _picker.pickImage(
      source: ImageSource.camera,
      imageQuality: 85,
      maxWidth: _maxLado,
      maxHeight: _maxLado,
    );
    return foto == null ? null : File(foto.path);
  }

  Future<File?> pickImageFromGallery() async {
    if (!await _ensurePermission(await _galleryPermission())) return null;
    final foto = await _picker.pickImage(
      source: ImageSource.gallery,
      imageQuality: 85,
      maxWidth: _maxLado,
      maxHeight: _maxLado,
    );
    return foto == null ? null : File(foto.path);
  }

  Future<bool> _ensurePermission(Permission permission) async {
    final status = await permission.status;
    if (status.isGranted || status.isLimited) return true;

    final result = await permission.request();
    return result.isGranted || result.isLimited;
  }

  /// La galería usa un permiso distinto según la versión de Android: desde
  /// la 13 (API 33) es Permission.photos (READ_MEDIA_IMAGES); en versiones
  /// anteriores es Permission.storage (READ_EXTERNAL_STORAGE) — pedir
  /// "photos" en un dispositivo viejo no solicita el permiso correcto. En
  /// iOS no hay distinción de versión, siempre es Permission.photos.
  Future<Permission> _galleryPermission() async {
    if (!Platform.isAndroid) return Permission.photos;

    final androidInfo = await DeviceInfoPlugin().androidInfo;
    return androidInfo.version.sdkInt >= 33
        ? Permission.photos
        : Permission.storage;
  }
}
