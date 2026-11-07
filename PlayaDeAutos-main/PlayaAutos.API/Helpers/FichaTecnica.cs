namespace PlayaAutos.API.Helpers
{
    /// <summary>
    /// Atributos técnicos del vehículo: valores permitidos, validación y el
    /// texto de "ficha técnica" que se antepone a la descripción en el
    /// catálogo público (la app lo muestra sin cambios en su pantalla de detalle).
    ///
    /// Los valores son listas cerradas (no texto libre) para que se puedan
    /// filtrar de forma confiable, por ejemplo desde el asistente de la app.
    /// </summary>
    public static class FichaTecnica
    {
        public static readonly string[] Transmisiones = { "Manual", "Automática", "CVT" };
        public static readonly string[] Combustibles = { "Nafta", "Diésel", "Flex", "Híbrido", "Eléctrico", "GNC" };
        public static readonly string[] Tracciones = { "4x2", "4x4", "AWD" };
        public const int PuertasMin = 2;
        public const int PuertasMax = 5;
        public const int MotorMaxLength = 30;

        /// <summary>
        /// Devuelve el valor canónico de la lista (ignorando mayúsculas y
        /// tildes), null si vino vacío, o lanza ArgumentException si no es válido.
        /// </summary>
        public static string? Normalizar(string? valor, string[] permitidos, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var buscado = SinTildes(valor.Trim());
            var canonico = permitidos.FirstOrDefault(p =>
                string.Equals(SinTildes(p), buscado, StringComparison.OrdinalIgnoreCase));

            return canonico ?? throw new ArgumentException(
                $"Valor de {campo.ToLowerInvariant()} no válido: \"{valor.Trim()}\". Valores permitidos: {string.Join(", ", permitidos)}.");
        }

        /// <summary>Valida y normaliza los 5 atributos. Devuelve un mensaje de error o null.</summary>
        public static string? Validar(
            ref string? transmision, ref string? combustible, ref string? traccion,
            int? cantidadPuertas, ref string? motor)
        {
            try
            {
                transmision = Normalizar(transmision, Transmisiones, "Transmisión");
                combustible = Normalizar(combustible, Combustibles, "Combustible");
                traccion = Normalizar(traccion, Tracciones, "Tracción");
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }

            if (cantidadPuertas is < PuertasMin or > PuertasMax)
                return $"La cantidad de puertas debe estar entre {PuertasMin} y {PuertasMax}.";

            motor = string.IsNullOrWhiteSpace(motor) ? null : motor.Trim();
            if (motor?.Length > MotorMaxLength)
                return $"El motor no puede superar los {MotorMaxLength} caracteres.";

            return null;
        }

        /// <summary>
        /// "Ficha técnica: Automática · Nafta · 4 puertas · Motor 1.8 · Tracción 4x2"
        /// + la descripción cargada a mano. Si no hay atributos, devuelve la
        /// descripción tal cual (los vehículos viejos se ven igual que antes).
        /// </summary>
        public static string? ArmarDescripcion(
            string? descripcion, string? transmision, string? combustible,
            int? cantidadPuertas, string? motor, string? traccion)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(transmision)) partes.Add(transmision);
            if (!string.IsNullOrWhiteSpace(combustible)) partes.Add(combustible);
            if (cantidadPuertas.HasValue) partes.Add($"{cantidadPuertas} puertas");
            if (!string.IsNullOrWhiteSpace(motor)) partes.Add($"Motor {motor}");
            if (!string.IsNullOrWhiteSpace(traccion)) partes.Add($"Tracción {traccion}");

            if (partes.Count == 0) return descripcion;

            var ficha = "Ficha técnica: " + string.Join(" · ", partes);
            return string.IsNullOrWhiteSpace(descripcion) ? ficha : $"{ficha}\n\n{descripcion.Trim()}";
        }

        private static string SinTildes(string s) => s
            .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u')
            .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U');
    }
}
