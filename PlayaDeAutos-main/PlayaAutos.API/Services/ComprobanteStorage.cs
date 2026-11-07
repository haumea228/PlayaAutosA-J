namespace PlayaAutos.API.Services
{
    public class ComprobanteInvalidoException : Exception
    {
        public ComprobanteInvalidoException(string message) : base(message) { }
    }

    /// <summary>
    /// Guarda las fotos de comprobantes de pago en disco, FUERA de wwwroot:
    /// un comprobante bancario no debe quedar accesible por una URL pública.
    /// Se sirven únicamente a través de endpoints autenticados.
    /// </summary>
    public class ComprobanteStorage
    {
        public const long TamanoMaximoBytes = 8 * 1024 * 1024; // 8 MB

        private readonly string _root;

        public ComprobanteStorage(IWebHostEnvironment env)
        {
            _root = Path.Combine(env.ContentRootPath, "App_Data", "comprobantes-cuotas");
            Directory.CreateDirectory(_root);
        }

        /// <summary>
        /// Valida y guarda la imagen. El tipo se determina por los bytes del
        /// archivo (no por la extensión ni el Content-Type que manda el cliente).
        /// </summary>
        public async Task<(string Archivo, string ContentType)> GuardarAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                throw new ComprobanteInvalidoException("Adjuntá la foto del comprobante.");
            if (archivo.Length > TamanoMaximoBytes)
                throw new ComprobanteInvalidoException("La imagen supera el tamaño máximo de 8 MB.");

            await using var input = archivo.OpenReadStream();
            var cabecera = new byte[12];
            var leidos = await input.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);

            var tipo = DetectarTipo(cabecera.AsSpan(0, leidos))
                ?? throw new ComprobanteInvalidoException("Formato no soportado. Enviá una imagen JPG, PNG o WEBP.");

            var nombre = $"{Guid.NewGuid():N}{tipo.Extension}";
            var destino = Path.Combine(_root, nombre);

            input.Position = 0;
            await using (var output = new FileStream(destino, FileMode.CreateNew, FileAccess.Write))
            {
                await input.CopyToAsync(output);
            }

            return (nombre, tipo.ContentType);
        }

        /// <summary>Ruta física del archivo, o null si el nombre es inválido o no existe.</summary>
        public string? ObtenerRuta(string archivo)
        {
            if (string.IsNullOrWhiteSpace(archivo) || Path.GetFileName(archivo) != archivo)
                return null;

            var ruta = Path.Combine(_root, archivo);
            return File.Exists(ruta) ? ruta : null;
        }

        public void Eliminar(string archivo)
        {
            var ruta = ObtenerRuta(archivo);
            if (ruta != null) File.Delete(ruta);
        }

        private static (string Extension, string ContentType)? DetectarTipo(ReadOnlySpan<byte> b)
        {
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
                return (".jpg", "image/jpeg");
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
                && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
                return (".png", "image/png");
            if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
                && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
                return (".webp", "image/webp");
            return null;
        }
    }
}
