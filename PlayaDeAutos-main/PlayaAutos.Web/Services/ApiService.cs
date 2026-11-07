namespace PlayaAutos.Web.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        private void AgregarToken()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            AgregarToken();
            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
        {
            AgregarToken();
            return await _httpClient.PostAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
        {
            AgregarToken();
            return await _httpClient.PutAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            AgregarToken();
            return await _httpClient.DeleteAsync(endpoint);
        }

        public async Task<HttpResponseMessage> GetRawAsync(string endpoint)
        {
            AgregarToken();
            return await _httpClient.GetAsync(endpoint);
        }

        /// <summary>
        /// Mensaje de error devuelto por la API (BadRequest("texto") llega como
        /// texto plano). Devuelve null si no hay un mensaje corto y legible.
        /// </summary>
        public static async Task<string?> LeerMensajeErrorAsync(HttpResponseMessage response)
        {
            var texto = (await response.Content.ReadAsStringAsync()).Trim();
            if (texto.Length == 0 || texto.Length > 300 || texto.StartsWith("{") || texto.StartsWith("<"))
                return null;
            return texto.Trim('"');
        }
    }
}