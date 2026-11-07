using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlayaAutos.API.Data;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Helpers;

namespace PlayaAutos.API.Services
{
    /// <summary>
    /// Configuración del asistente (sección "Asistente" de appsettings).
    /// Usa la API de chat compatible con OpenAI, que ofrecen Groq, Gemini y
    /// Ollama: cambiar de proveedor es cambiar BaseUrl, Modelos y la clave.
    /// </summary>
    public class AsistenteOptions
    {
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";

        /// <summary>Se prueban en orden: si uno falla o agota su límite, se usa el siguiente.</summary>
        public List<string> Modelos { get; set; } = new();

        /// <summary>Archivo con la clave (relativo al ContentRoot). Vacío = sin clave (p. ej. Ollama).</summary>
        public string? ApiKeyPath { get; set; }

        /// <summary>"low" para modelos de razonamiento (gpt-oss). Null = no se envía.</summary>
        public string? ReasoningEffort { get; set; }

        /// <summary>"schema" (JSON estricto), "object" (JSON libre) o "ninguno".</summary>
        public string ModoJson { get; set; } = "schema";

        public int TimeoutSegundos { get; set; } = 20;
        public int MaxRecomendaciones { get; set; } = 4;
    }

    /// <summary>
    /// Asistente que recomienda vehículos del catálogo a partir de una consulta
    /// en lenguaje natural. El modelo recibe SOLO los vehículos disponibles y
    /// devuelve {mensaje, ids}; la API valida que cada id exista en el catálogo
    /// antes de devolverlo, así el asistente no puede inventar vehículos.
    /// </summary>
    public class AsistenteService
    {
        private static readonly JsonSerializerOptions JsonCompacto = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient _http;
        private readonly AppDbContext _context;
        private readonly AsistenteOptions _opciones;
        private readonly ILogger<AsistenteService> _logger;
        private readonly string? _apiKey;

        public AsistenteService(HttpClient http, AppDbContext context, IOptions<AsistenteOptions> opciones,
            IWebHostEnvironment env, ILogger<AsistenteService> logger)
        {
            _http = http;
            _context = context;
            _opciones = opciones.Value;
            _logger = logger;

            _http.BaseAddress = new Uri(_opciones.BaseUrl.EndsWith('/') ? _opciones.BaseUrl : _opciones.BaseUrl + "/");
            _http.Timeout = TimeSpan.FromSeconds(_opciones.TimeoutSegundos);

            if (!string.IsNullOrWhiteSpace(_opciones.ApiKeyPath))
            {
                var ruta = Path.IsPathRooted(_opciones.ApiKeyPath)
                    ? _opciones.ApiKeyPath
                    : Path.Combine(env.ContentRootPath, _opciones.ApiKeyPath);
                if (File.Exists(ruta)) _apiKey = File.ReadAllText(ruta).Trim();
                else _logger.LogWarning("Asistente: no se encontró la clave en {Ruta}; se usará solo el filtro local.", ruta);
            }
        }

        public async Task<RespuestaAsistenteDto> ConsultarAsync(ConsultaAsistenteDto consulta, CancellationToken ct)
        {
            var catalogo = await ObtenerCatalogoAsync(ct);
            if (catalogo.Count == 0)
                return new RespuestaAsistenteDto { Mensaje = "Por ahora no hay vehículos disponibles en el catálogo.", Fuente = "filtro" };

            var deIa = await ConsultarModelosAsync(consulta, catalogo, ct);
            if (deIa != null) return Armar(deIa.Value.Mensaje, deIa.Value.Ids, catalogo, "ia");

            var (mensaje, ids) = FiltroLocalAsistente.Recomendar(consulta.Pregunta, catalogo, _opciones.MaxRecomendaciones);
            return Armar(mensaje, ids, catalogo, "filtro");
        }

        private RespuestaAsistenteDto Armar(string mensaje, IEnumerable<int> ids, List<VehiculoDto> catalogo, string fuente)
        {
            var porId = catalogo.ToDictionary(v => v.VehiculoId);
            return new RespuestaAsistenteDto
            {
                Mensaje = mensaje,
                // Solo ids que existen en el catálogo disponible, sin repetir y con tope.
                Vehiculos = ids.Distinct().Where(porId.ContainsKey).Take(_opciones.MaxRecomendaciones)
                    .Select(id => porId[id]).ToList(),
                Fuente = fuente
            };
        }

        private async Task<(string Mensaje, List<int> Ids)?> ConsultarModelosAsync(
            ConsultaAsistenteDto consulta, List<VehiculoDto> catalogo, CancellationToken ct)
        {
            if (_apiKey == null && _opciones.BaseUrl.Contains("groq.com")) return null;

            var mensajes = ArmarMensajes(consulta, catalogo);

            foreach (var modelo in _opciones.Modelos)
            {
                try
                {
                    var resultado = await LlamarModeloAsync(modelo, mensajes, ct);
                    if (resultado != null) return resultado;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    // Timeout, red, respuesta con forma inesperada, JSON inválido...:
                    // se prueba el siguiente modelo (o el filtro local).
                    _logger.LogWarning(ex, "Asistente: falló el modelo {Modelo}.", modelo);
                }
            }
            return null;
        }

        private async Task<(string Mensaje, List<int> Ids)?> LlamarModeloAsync(string modelo, List<object> mensajes, CancellationToken ct)
        {
            var cuerpo = new Dictionary<string, object?>
            {
                ["model"] = modelo,
                ["messages"] = mensajes,
                ["temperature"] = 0.2,
                ["max_completion_tokens"] = 1200,
                ["reasoning_effort"] = _opciones.ReasoningEffort,
                ["response_format"] = _opciones.ModoJson switch
                {
                    "schema" => new
                    {
                        type = "json_schema",
                        json_schema = new
                        {
                            name = "recomendacion",
                            strict = true,
                            schema = new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = new[] { "mensaje", "ids" },
                                properties = new
                                {
                                    mensaje = new { type = "string" },
                                    ids = new { type = "array", items = new { type = "integer" } }
                                }
                            }
                        }
                    },
                    "object" => new { type = "json_object" },
                    _ => null
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(cuerpo, JsonCompacto), Encoding.UTF8, "application/json")
            };
            if (_apiKey != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Asistente: {Modelo} respondió {Status}: {Detalle}",
                    modelo, (int)response.StatusCode, detalle.Length > 300 ? detalle[..300] : detalle);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var contenido = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return Interpretar(contenido);
        }

        /// <summary>Extrae {mensaje, ids} aunque el modelo agregue texto alrededor del JSON.</summary>
        private static (string Mensaje, List<int> Ids)? Interpretar(string? contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido)) return null;
            var inicio = contenido.IndexOf('{');
            var fin = contenido.LastIndexOf('}');
            if (inicio < 0 || fin <= inicio) return null;

            using var json = JsonDocument.Parse(contenido[inicio..(fin + 1)]);
            var raiz = json.RootElement;
            if (!raiz.TryGetProperty("mensaje", out var m) || m.ValueKind != JsonValueKind.String) return null;

            var ids = new List<int>();
            if (raiz.TryGetProperty("ids", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var e in arr.EnumerateArray())
                    if (e.TryGetInt32(out var id)) ids.Add(id);

            var mensaje = m.GetString()!.Trim();
            return mensaje.Length == 0 ? null : (mensaje, ids);
        }

        private List<object> ArmarMensajes(ConsultaAsistenteDto consulta, List<VehiculoDto> catalogo)
        {
            var fichas = catalogo.Select(v => new
            {
                id = v.VehiculoId,
                marca = v.Marca,
                modelo = v.Modelo,
                anio = v.Anio,
                tipo = v.Tipo,
                condicion = v.Condicion,
                color = v.Color,
                km = v.Kilometraje,
                precioGs = v.PrecioVenta,
                transmision = v.Transmision,
                combustible = v.Combustible,
                puertas = v.CantidadPuertas,
                motor = v.Motor,
                traccion = v.Traccion
            });

            var sistema = $$"""
                Sos el asistente de A&J Autos Usados (Paraguay). Ayudás a clientes a elegir vehículos del catálogo.
                Reglas:
                - Recomendá SOLO vehículos del catálogo de abajo, por su id. Nunca inventes vehículos ni datos.
                - Respetá estrictamente los filtros numéricos del cliente (km, precio, año). Los precios están en guaraníes (Gs.).
                - Si ninguno cumple, decilo con amabilidad y sugerí la alternativa más cercana, aclarando que es una alternativa.
                - Pedir una marca o modelo que no está en el catálogo SÍ es una consulta sobre vehículos: decí que no hay y ofrecé las alternativas más parecidas (tipo, precio o estilo), incluyendo sus ids.
                - La transmisión CVT es un tipo de transmisión automática.
                - Si la pregunta no tiene nada que ver con vehículos, respondé brevemente que solo podés ayudar con el catálogo.
                - Mensaje breve (máximo 3 oraciones), tratando de "vos", sin listas ni markdown: las fichas se muestran aparte.
                - Nombrá marca y modelo de lo que recomendás. Nunca menciones ids ni números de id en el mensaje.
                - Ordená los ids del más al menos recomendado. Máximo {{_opciones.MaxRecomendaciones}}.
                - Respondé SIEMPRE y SOLO con un objeto JSON: {"mensaje": "...", "ids": [..]}. Si no recomendás ninguno, usá "ids": [].
                Catálogo disponible (JSON):
                {{JsonSerializer.Serialize(fichas, JsonCompacto)}}
                """;

            var mensajes = new List<object> { new { role = "system", content = sistema } };

            // Últimos turnos de la conversación abierta en la app (no se guardan).
            foreach (var h in (consulta.Historial ?? new()).TakeLast(6))
            {
                var rol = h.Rol == "asistente" ? "assistant" : "user";
                var texto = h.Texto.Length > 1000 ? h.Texto[..1000] : h.Texto;
                mensajes.Add(new { role = rol, content = texto });
            }
            mensajes.Add(new { role = "user", content = consulta.Pregunta });
            return mensajes;
        }

        /// <summary>Vehículos visibles en el catálogo, con la misma forma que GET api/vehiculos/catalogo.</summary>
        private async Task<List<VehiculoDto>> ObtenerCatalogoAsync(CancellationToken ct)
        {
            return await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Estado.VisibleEnCatalogo)
                .OrderBy(v => v.VehiculoId)
                .Select(v => new VehiculoDto
                {
                    VehiculoId = v.VehiculoId,
                    CodigoInterno = v.CodigoInterno,
                    Marca = v.Modelo.Marca.Nombre,
                    Modelo = v.Modelo.Nombre,
                    Tipo = v.Tipo.Descripcion,
                    Condicion = v.Condicion.Descripcion,
                    Estado = v.Estado.Descripcion,
                    Anio = v.Anio,
                    Color = v.Color,
                    Kilometraje = v.Kilometraje,
                    PrecioVenta = v.PrecioVenta,
                    Descripcion = FichaTecnica.ArmarDescripcion(v.Descripcion,
                        v.Transmision, v.Combustible, v.CantidadPuertas, v.Motor, v.Traccion),
                    Transmision = v.Transmision,
                    Combustible = v.Combustible,
                    CantidadPuertas = v.CantidadPuertas,
                    Motor = v.Motor,
                    Traccion = v.Traccion,
                    FotoPrincipal = v.Fotos.Where(f => f.EsPrincipal).Select(f => f.URL).FirstOrDefault()
                })
                .ToListAsync(ct);
        }
    }
}
