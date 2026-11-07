using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlayaAutos.API.DTOs;
using PlayaAutos.API.Services;

namespace PlayaAutos.API.Controllers
{
    /// <summary>
    /// Asistente de recomendaciones del catálogo. Público, como el catálogo, pero
    /// con límite de consultas por IP para no agotar el plan gratuito del modelo.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AsistenteController : ControllerBase
    {
        public const string PoliticaLimite = "asistente";
        private const int MaxPregunta = 500;

        private readonly AsistenteService _asistente;

        public AsistenteController(AsistenteService asistente)
        {
            _asistente = asistente;
        }

        // POST: api/asistente/consultar
        [HttpPost("consultar")]
        [EnableRateLimiting(PoliticaLimite)]
        public async Task<ActionResult<RespuestaAsistenteDto>> Consultar(ConsultaAsistenteDto dto, CancellationToken ct)
        {
            var pregunta = dto.Pregunta?.Trim() ?? "";
            if (pregunta.Length == 0) return BadRequest("Escribí tu consulta.");
            if (pregunta.Length > MaxPregunta) return BadRequest($"La consulta no puede superar los {MaxPregunta} caracteres.");

            dto.Pregunta = pregunta;
            dto.Historial = dto.Historial?
                .Where(h => !string.IsNullOrWhiteSpace(h.Texto) && (h.Rol == "usuario" || h.Rol == "asistente"))
                .ToList();

            return await _asistente.ConsultarAsync(dto, ct);
        }
    }
}
