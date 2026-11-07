import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../../config/router/app_router.dart';
import '../network/dio_client.dart';

/// Aviso recibido o tocado por el usuario. [tipo] es "cita" o "cuota" (lo
/// manda la API en los datos del push) y sirve para refrescar la pantalla
/// que corresponda.
class NotificacionEvento {
  const NotificacionEvento({required this.tipo, this.id});

  final String tipo;
  final int? id;

  factory NotificacionEvento.desdeDatos(Map<String, dynamic> datos) {
    return NotificacionEvento(
      tipo: datos['tipo']?.toString() ?? '',
      id: int.tryParse(datos['id']?.toString() ?? ''),
    );
  }
}

/// Requerido por firebase_messaging para mensajes con la app en segundo plano
/// o cerrada. Los mensajes de la API traen "notification", así que Android
/// los muestra solo; acá no hace falta hacer nada más.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
}

/// Notificaciones push (Firebase Cloud Messaging):
/// - registra el token del dispositivo en la API al iniciar sesión y lo da de
///   baja al cerrarla;
/// - con la app abierta, muestra el aviso como notificación local (Android no
///   muestra los push de FCM en primer plano por sí solo);
/// - al tocar un aviso, abre Mis Citas o Mis Cuotas.
///
/// Si Firebase no está configurado (falta google-services.json), queda
/// deshabilitado y la app funciona igual, sin notificaciones.
class NotificationService {
  NotificationService._();

  static final NotificationService instance = NotificationService._();

  /// Mismo id que FirebasePushSender.CanalAndroid en la API.
  static const String _canalId = 'playaautos_general';
  static const AndroidNotificationChannel _canal = AndroidNotificationChannel(
    _canalId,
    'Avisos de PlayaAutos',
    description: 'Confirmaciones de citas y pagos de cuotas',
    importance: Importance.high,
  );

  final FlutterLocalNotificationsPlugin _local =
      FlutterLocalNotificationsPlugin();
  final StreamController<NotificacionEvento> _eventos =
      StreamController<NotificacionEvento>.broadcast();

  bool _disponible = false;
  int _idLocal = 0;

  bool get disponible => _disponible;

  /// Cada aviso recibido (app abierta) o tocado. La app lo usa para refrescar
  /// los datos de citas/cuotas.
  Stream<NotificacionEvento> get eventos => _eventos.stream;

  Future<void> inicializar() async {
    if (kIsWeb || !(Platform.isAndroid || Platform.isIOS)) return;

    try {
      await Firebase.initializeApp();
    } catch (e) {
      debugPrint('Notificaciones deshabilitadas: Firebase no está configurado ($e).');
      return;
    }
    _disponible = true;

    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

    await _local.initialize(
      settings: const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
        iOS: DarwinInitializationSettings(
          requestAlertPermission: false,
          requestBadgePermission: false,
          requestSoundPermission: false,
        ),
      ),
      onDidReceiveNotificationResponse: (respuesta) =>
          _abrirDesdePayload(respuesta.payload),
    );

    await _local
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(_canal);

    // iOS: mostrar el aviso aunque la app esté abierta.
    await FirebaseMessaging.instance.setForegroundNotificationPresentationOptions(
      alert: true,
      badge: true,
      sound: true,
    );

    FirebaseMessaging.onMessage.listen(_mostrarEnPrimerPlano);
    FirebaseMessaging.onMessageOpenedApp.listen((m) => _abrir(m.data));
    FirebaseMessaging.instance.onTokenRefresh.listen((token) async {
      if (await DioClient.getToken() != null) await _enviarToken(token);
    });

    // La app estaba cerrada y se abrió tocando un aviso.
    final inicial = await FirebaseMessaging.instance.getInitialMessage();
    if (inicial != null) _abrirDespuesDelPrimerFrame(inicial.data);

    final lanzamiento = await _local.getNotificationAppLaunchDetails();
    if (lanzamiento?.didNotificationLaunchApp ?? false) {
      final payload = lanzamiento!.notificationResponse?.payload;
      WidgetsBinding.instance.addPostFrameCallback((_) => _abrirDesdePayload(payload));
    }
  }

  /// Pide permiso (Android 13+ / iOS) y registra el token en la API. Se llama
  /// después de iniciar sesión o al abrir la app con la sesión ya iniciada.
  Future<void> registrarDispositivo() async {
    if (!_disponible) return;
    try {
      final permiso = await FirebaseMessaging.instance.requestPermission();
      if (permiso.authorizationStatus == AuthorizationStatus.denied) return;

      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) await _enviarToken(token);
    } catch (e) {
      debugPrint('No se pudo registrar el dispositivo para notificaciones: $e');
    }
  }

  /// Da de baja el token en la API. Debe llamarse ANTES de borrar el JWT.
  Future<void> desregistrarDispositivo() async {
    if (!_disponible) return;
    try {
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) {
        await DioClient.delete('MiCuenta/dispositivos', data: {'token': token});
      }
    } catch (e) {
      debugPrint('No se pudo dar de baja el dispositivo: $e');
    }
  }

  Future<void> _enviarToken(String token) async {
    try {
      await DioClient.post(
        'MiCuenta/dispositivos',
        data: {'token': token, 'plataforma': Platform.isIOS ? 'ios' : 'android'},
      );
    } catch (e) {
      debugPrint('No se pudo enviar el token de notificaciones: $e');
    }
  }

  Future<void> _mostrarEnPrimerPlano(RemoteMessage mensaje) async {
    _eventos.add(NotificacionEvento.desdeDatos(mensaje.data));

    final notificacion = mensaje.notification;
    if (notificacion == null || !Platform.isAndroid) return;

    await _local.show(
      id: _idLocal++,
      title: notificacion.title,
      body: notificacion.body,
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          _canal.id,
          _canal.name,
          channelDescription: _canal.description,
          importance: Importance.high,
          priority: Priority.high,
          styleInformation: BigTextStyleInformation(notificacion.body ?? ''),
        ),
      ),
      payload: jsonEncode(mensaje.data),
    );
  }

  void _abrirDesdePayload(String? payload) {
    if (payload == null || payload.isEmpty) return;
    try {
      _abrir(Map<String, dynamic>.from(jsonDecode(payload) as Map));
    } catch (_) {}
  }

  void _abrirDespuesDelPrimerFrame(Map<String, dynamic> datos) {
    WidgetsBinding.instance.addPostFrameCallback((_) => _abrir(datos));
  }

  void _abrir(Map<String, dynamic> datos) {
    final evento = NotificacionEvento.desdeDatos(datos);
    _eventos.add(evento);

    switch (evento.tipo) {
      case 'cita':
        AppRouter.router.go('/mis-citas');
      case 'cuota':
        AppRouter.router.go('/mis-cuotas');
    }
  }
}
