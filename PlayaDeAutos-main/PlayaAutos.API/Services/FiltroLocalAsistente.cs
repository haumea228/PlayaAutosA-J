using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PlayaAutos.API.DTOs;

namespace PlayaAutos.API.Services
{
    /// <summary>
    /// Respaldo del asistente cuando el modelo de IA no está disponible (sin
    /// clave, límite gratuito agotado, sin internet). Interpreta las consultas
    /// más comunes con reglas simples: marca/modelo/tipo, kilometraje, precio,
    /// año, transmisión, combustible, tracción y puertas.
    /// </summary>
    public static class FiltroLocalAsistente
    {
        public static (string Mensaje, List<int> Ids) Recomendar(string pregunta, IReadOnlyList<VehiculoDto> catalogo, int maximo)
        {
            var texto = Normalizar(pregunta);
            var criterios = 0;
            IEnumerable<VehiculoDto> resultado = catalogo;

            // Marca / modelo (tomados del propio catálogo)
            var marcas = catalogo.Select(v => v.Marca).Distinct()
                .Where(m => ContienePalabra(texto, Normalizar(m)) || ContienePalabra(texto, Normalizar(m) + "s")).ToList();
            if (marcas.Count > 0) { resultado = resultado.Where(v => marcas.Contains(v.Marca)); criterios++; }

            var modelos = catalogo.Select(v => v.Modelo).Distinct()
                .Where(m => ContienePalabra(texto, Normalizar(m))).ToList();
            if (modelos.Count > 0) { resultado = resultado.Where(v => modelos.Contains(v.Modelo)); criterios++; }

            // Tipo ("camioneta" abarca SUV y Pickup)
            var tipos = catalogo.Select(v => v.Tipo).Distinct()
                .Where(t => ContienePalabra(texto, Normalizar(t)) || ContienePalabra(texto, Normalizar(t) + "s")).ToList();
            if (Regex.IsMatch(texto, @"\bcamionetas?\b"))
                tipos.AddRange(catalogo.Select(v => v.Tipo).Distinct().Where(t => Normalizar(t) is "suv" or "pickup"));
            if (tipos.Count > 0) { resultado = resultado.Where(v => tipos.Contains(v.Tipo)); criterios++; }

            // Atributos de ficha técnica
            var transmision = Primero(texto, ("automatic", "Automática"), ("manual", "Manual"), ("mecanic", "Manual"), ("cvt", "CVT"));
            if (transmision != null)
            {
                // La CVT es un tipo de transmisión automática.
                resultado = transmision == "Automática"
                    ? resultado.Where(v => v.Transmision is "Automática" or "CVT")
                    : resultado.Where(v => v.Transmision == transmision);
                criterios++;
            }

            var combustible = Primero(texto, ("diesel", "Diésel"), ("gasoil", "Diésel"), ("nafta", "Nafta"), ("gasolina", "Nafta"),
                ("hibrid", "Híbrido"), ("electric", "Eléctrico"), ("flex", "Flex"), ("gnc", "GNC"));
            if (combustible != null) { resultado = resultado.Where(v => v.Combustible == combustible); criterios++; }

            var traccion = Primero(texto, ("4x4", "4x4"), ("awd", "AWD"), ("4x2", "4x2"));
            if (traccion != null) { resultado = resultado.Where(v => v.Traccion == traccion); criterios++; }

            var puertas = Regex.Match(texto, @"\b([2-5])\s*puertas\b");
            if (puertas.Success) { var p = int.Parse(puertas.Groups[1].Value); resultado = resultado.Where(v => v.CantidadPuertas == p); criterios++; }

            // Kilometraje: "menos de 120 mil km", "hasta 100.000 kilometros"
            var km = Regex.Match(texto, @"(menos|hasta|maximo|max|no mas|por debajo|debajo)\s*(?:de\s*)?(\d[\d\.]*)\s*(mil|k)?\s*(km|kms|kilometros|kilometraje)");
            if (km.Success) { var lim = Numero(km.Groups[2].Value, km.Groups[3].Value); resultado = resultado.Where(v => v.Kilometraje <= lim); criterios++; }

            // Precio: "menos de 40 millones", "hasta 35.000.000 gs"
            var precio = Regex.Match(texto, @"(menos|hasta|maximo|max|no mas|por debajo|debajo)\s*(?:de\s*)?(\d[\d\.]*)\s*(millones|millon|mill|m\b|gs|guaranies)");
            if (precio.Success)
            {
                var valor = Numero(precio.Groups[2].Value, "");
                var lim = precio.Groups[3].Value.StartsWith("m") ? valor * 1_000_000 : valor;
                resultado = resultado.Where(v => v.PrecioVenta <= lim); criterios++;
            }

            // Año: "desde 2010", "2010 en adelante", "mas nuevo que 2008"
            var anio = Regex.Match(texto, @"(desde|a partir de|posterior a|mas nuevo que|del)\s*((19|20)\d{2})|((19|20)\d{2})\s*(en adelante|o mas nuevo)");
            if (anio.Success)
            {
                var a = int.Parse(anio.Groups[2].Success && anio.Groups[2].Value.Length > 0 ? anio.Groups[2].Value : anio.Groups[4].Value);
                resultado = resultado.Where(v => v.Anio >= a); criterios++;
            }

            var lista = resultado.OrderBy(v => v.PrecioVenta).Take(maximo).ToList();

            if (criterios == 0)
                return ("No pude interpretar tu consulta. Probá con algo como \"Toyota con menos de 120 mil km\" o \"automático de menos de 40 millones\".",
                    new List<int>());

            return lista.Count == 0
                ? ("No encontré vehículos disponibles que cumplan todo lo que pediste. Probá ampliando algún criterio.", new List<int>())
                : ($"Encontré {lista.Count} {(lista.Count == 1 ? "vehículo que coincide" : "vehículos que coinciden")} con lo que buscás.",
                    lista.Select(v => v.VehiculoId).ToList());
        }

        private static string? Primero(string texto, params (string Clave, string Valor)[] opciones) =>
            opciones.FirstOrDefault(o => texto.Contains(o.Clave)).Valor;

        private static bool ContienePalabra(string texto, string palabra) =>
            palabra.Length > 0 && Regex.IsMatch(texto, $@"(^|[^a-z0-9]){Regex.Escape(palabra)}($|[^a-z0-9])");

        private static long Numero(string digitos, string multiplicador)
        {
            var n = long.Parse(digitos.Replace(".", ""));
            return multiplicador is "mil" or "k" ? n * 1000 : n;
        }

        public static string Normalizar(string s)
        {
            var sinTildes = new StringBuilder();
            foreach (var c in s.ToLowerInvariant().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sinTildes.Append(c);
            return sinTildes.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
