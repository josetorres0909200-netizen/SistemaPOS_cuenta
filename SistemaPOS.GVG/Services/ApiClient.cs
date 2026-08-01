using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SistemaPOS.Desktop.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7269/api/";
        private string? _jwtToken;

        public ApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Establece el token JWT para autenticación
        /// </summary>
        public void SetAuthToken(string token)
        {
            _jwtToken = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Limpia el token de autenticación
        /// </summary>
        public void ClearAuthToken()
        {
            _jwtToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        /// <summary>
        /// Verifica si hay un token establecido
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken);

        // Método genérico para consultar datos
        public async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>() 
                    ?? throw new InvalidOperationException("Respuesta vacía del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al consultar el endpoint '{endpoint}': {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error desconocido al consultar '{endpoint}': {ex.Message}", ex);
            }
        }

        // Obtener un recurso por ID
        public async Task<T> GetByIdAsync<T>(string endpoint, int id)
        {
            return await GetAsync<T>($"{endpoint}/{id}");
        }

        // Búsqueda especial por código de barras
        public async Task<T> GetByCodigoBarrasAsync<T>(string codigoBarras)
        {
            return await GetAsync<T>($"productos/buscar/codigo/{codigoBarras}");
        }

        // Método genérico para enviar datos
        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>() 
                    ?? throw new InvalidOperationException("Respuesta vacía del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al enviar datos a '{endpoint}': {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error desconocido al enviar datos a '{endpoint}': {ex.Message}", ex);
            }
        }

        // Método genérico para actualizar datos
        public async Task<T> PutAsync<T>(string endpoint, int id, object data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{id}", data);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>() 
                    ?? throw new InvalidOperationException("Respuesta vacía del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al actualizar en '{endpoint}/{id}': {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error desconocido al actualizar '{endpoint}': {ex.Message}", ex);
            }
        }

        // Método genérico para eliminar datos
        public async Task<bool> DeleteAsync(string endpoint, int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al eliminar en '{endpoint}/{id}': {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error desconocido al eliminar en '{endpoint}': {ex.Message}", ex);
            }
        }
    }
}
