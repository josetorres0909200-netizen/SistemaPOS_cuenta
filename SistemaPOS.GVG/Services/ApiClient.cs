using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using SistemaPOS.GVG;

namespace SistemaPOS.Desktop.Services
{
    public class ApiClient
    {
        // Singleton: instancia única compartida
        private static ApiClient? _instance;
        private static readonly object _lock = new object();

        public static ApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ApiClient();
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7269/api/";
        private string? _jwtToken;

        // Constructor privado para Singleton
        private ApiClient()
        {
            // ✅ Omitir validación de certificado SSL para desarrollo (localhost)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler);
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
        /// Restaura el token desde App.Properties (descifra token DPAPI)
        /// </summary>
        public bool RestoreAuthTokenFromSession()
        {
            try
            {
                if (App.Current.Properties.Contains("Token"))
                {
                    string encryptedToken = App.Current.Properties["Token"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(encryptedToken))
                    {
                        string decryptedToken = TokenSecurityHelper.UnprotectToken(encryptedToken);
                        SetAuthToken(decryptedToken);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
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

        /// <summary>
        /// Verifica si el token JWT ha expirado
        /// </summary>
        public bool IsTokenExpired()
        {
            if (string.IsNullOrEmpty(_jwtToken))
                return true;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(_jwtToken);

                // Verificar si el token ha expirado (con margen de 30 segundos)
                return token.ValidTo.AddSeconds(-30) < DateTime.UtcNow;
            }
            catch
            {
                // Si hay error al leer el token, considerarlo expirado
                return true;
            }
        }

        // Método genérico para consultar datos
        public async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                // ✅ Validar expiración del token antes de la petición
                if (IsTokenExpired())
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

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
                // ✅ Validar expiración del token antes de la petición
                if (IsTokenExpired())
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

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
                // ✅ Validar expiración del token antes de la petición
                if (IsTokenExpired())
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

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
                // ✅ Validar expiración del token antes de la petición
                if (IsTokenExpired())
                {
                    throw new UnauthorizedAccessException("Sesión expirada. Por favor inicie sesión nuevamente.");
                }

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
