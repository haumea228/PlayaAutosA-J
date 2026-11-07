using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using PlayaAutos.API.Data;

namespace PlayaAutos.API.Services
{
    /// <summary>
    /// Envío de push vía Firebase Cloud Messaging. Singleton: inicializa la app
    /// de Firebase una sola vez con la cuenta de servicio indicada en
    /// "Firebase:CredentialsPath" (relativa al ContentRoot). Si el archivo no
    /// existe, queda deshabilitado y no se envía nada: la API funciona igual.
    /// </summary>
    public class FirebasePushSender
    {
        /// <summary>Debe coincidir con el canal que crea la app Android.</summary>
        public const string CanalAndroid = "playaautos_general";

        private readonly FirebaseMessaging? _messaging;
        private readonly ILogger<FirebasePushSender> _logger;

        public bool Habilitado => _messaging != null;

        public FirebasePushSender(IConfiguration config, IWebHostEnvironment env, ILogger<FirebasePushSender> logger)
        {
            _logger = logger;

            var ruta = config["Firebase:CredentialsPath"];
            if (string.IsNullOrWhiteSpace(ruta))
            {
                _logger.LogWarning("Notificaciones push deshabilitadas: falta Firebase:CredentialsPath en la configuración.");
                return;
            }

            var completa = Path.IsPathRooted(ruta) ? ruta : Path.Combine(env.ContentRootPath, ruta);
            if (!File.Exists(completa))
            {
                _logger.LogWarning("Notificaciones push deshabilitadas: no se encontró la cuenta de servicio de Firebase en {Ruta}.", completa);
                return;
            }

            try
            {
                var credencial = CredentialFactory
                    .FromFile<ServiceAccountCredential>(completa)
                    .ToGoogleCredential();

                var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions { Credential = credencial });
                _messaging = FirebaseMessaging.GetMessaging(app);
                _logger.LogInformation("Notificaciones push habilitadas (Firebase).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo inicializar Firebase; las notificaciones push quedan deshabilitadas.");
            }
        }

        /// <summary>
        /// Envía la notificación a los tokens dados. Devuelve los tokens que FCM
        /// informó como inválidos o dados de baja, para eliminarlos.
        /// </summary>
        public async Task<IReadOnlyList<string>> EnviarAsync(
            IReadOnlyList<string> tokens, string titulo, string cuerpo, IReadOnlyDictionary<string, string> datos)
        {
            if (_messaging == null || tokens.Count == 0) return Array.Empty<string>();

#pragma warning disable CS0618 // Tokens = tokens de registro FCM, que es lo que entrega firebase_messaging en la app.
            var mensaje = new MulticastMessage
            {
                Tokens = tokens,
#pragma warning restore CS0618
                Notification = new Notification { Title = titulo, Body = cuerpo },
                Data = datos,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification { ChannelId = CanalAndroid }
                },
                Apns = new ApnsConfig { Aps = new Aps { Sound = "default" } }
            };

            var respuesta = await _messaging.SendEachForMulticastAsync(mensaje);

            var invalidos = new List<string>();
            for (var i = 0; i < respuesta.Responses.Count; i++)
            {
                var r = respuesta.Responses[i];
                if (r.IsSuccess) continue;

                var codigo = r.Exception?.MessagingErrorCode;
                if (codigo is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument or MessagingErrorCode.SenderIdMismatch)
                    invalidos.Add(tokens[i]);
                else
                    _logger.LogWarning(r.Exception, "Fallo al enviar push a un dispositivo ({Codigo}).", codigo);
            }
            return invalidos;
        }
    }

    /// <summary>
    /// Notificaciones a clientes. Nunca lanza excepciones: un problema con el
    /// envío de push no debe hacer fallar la operación de negocio (aprobar un
    /// pago, confirmar una cita), que ya quedó guardada.
    /// </summary>
    public class NotificacionesService
    {
        private readonly AppDbContext _context;
        private readonly FirebasePushSender _sender;
        private readonly ILogger<NotificacionesService> _logger;

        public NotificacionesService(AppDbContext context, FirebasePushSender sender, ILogger<NotificacionesService> logger)
        {
            _context = context;
            _sender = sender;
            _logger = logger;
        }

        public async Task NotificarUsuarioAsync(int? usuarioId, string titulo, string cuerpo, string tipo, int referenciaId)
        {
            if (usuarioId == null || !_sender.Habilitado) return;

            try
            {
                var tokens = await _context.DispositivosNotificacion
                    .Where(d => d.UsuarioId == usuarioId)
                    .Select(d => d.Token)
                    .ToListAsync();
                if (tokens.Count == 0) return;

                var datos = new Dictionary<string, string>
                {
                    ["tipo"] = tipo,
                    ["id"] = referenciaId.ToString()
                };

                var invalidos = await _sender.EnviarAsync(tokens, titulo, cuerpo, datos);
                if (invalidos.Count > 0)
                {
                    await _context.DispositivosNotificacion
                        .Where(d => invalidos.Contains(d.Token))
                        .ExecuteDeleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar la notificación '{Titulo}' al usuario {UsuarioId}.", titulo, usuarioId);
            }
        }
    }
}
