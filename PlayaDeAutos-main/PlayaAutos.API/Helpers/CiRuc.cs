using System.Text.RegularExpressions;

namespace PlayaAutos.API.Helpers
{
    /// <summary>
    /// Formato del CI/RUC que carga el cliente desde la app: CI de 5 a 8
    /// números, o RUC con su dígito verificador separado por guion
    /// (p. ej. "1234567" o "80012345-6"). Se quitan puntos y espacios.
    /// </summary>
    public static partial class CiRuc
    {
        public const string MensajeFormato =
            "El CI/RUC debe tener entre 5 y 8 números, sin puntos (para RUC, agregá el dígito verificador con guion, p. ej. 80012345-6).";

        [GeneratedRegex(@"^\d{5,8}(-\d)?$")]
        private static partial Regex Patron();

        /// <summary>Quita espacios y puntos; null si queda vacío.</summary>
        public static string? Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var limpio = valor.Replace(".", "").Replace(" ", "").Trim();
            return limpio.Length == 0 ? null : limpio;
        }

        public static bool EsValido(string valorNormalizado) => Patron().IsMatch(valorNormalizado);
    }
}
