namespace PlayaAutos.API.DTOs
{
    public class MensajeAsistenteDto
    {
        /// <summary>"usuario" | "asistente"</summary>
        public string Rol { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
    }

    public class ConsultaAsistenteDto
    {
        public string Pregunta { get; set; } = string.Empty;

        /// <summary>
        /// Mensajes previos de la conversación abierta en la app (no se guardan
        /// en el servidor). Permite repreguntas como "¿y más baratos?".
        /// </summary>
        public List<MensajeAsistenteDto>? Historial { get; set; }
    }

    public class RespuestaAsistenteDto
    {
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>Vehículos recomendados, con la misma forma que el catálogo.</summary>
        public List<VehiculoDto> Vehiculos { get; set; } = new();

        /// <summary>"ia" si respondió el modelo, "filtro" si se usó el filtro local de respaldo.</summary>
        public string Fuente { get; set; } = "ia";
    }
}
