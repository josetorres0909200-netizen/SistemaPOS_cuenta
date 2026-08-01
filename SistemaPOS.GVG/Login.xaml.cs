using SistemaPOS.GVG;
using SistemaPOS.Desktop;
using SistemaPOS.Desktop.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace SistemaPOS.GVG.Views
{
    public partial class Login : Window
    {
        private readonly ApiClient _apiClient;

        public Login()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private async void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Por favor ingrese su usuario y contraseña.", 
                    "Campos vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnIngresar.IsEnabled = false;
                btnIngresar.Content = "Autenticando...";

                // Preparar datos de login
                var loginData = new { Username = usuario, Password = password };

                // Llamar a la API de autenticación
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://localhost:7269");
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var json = JsonSerializer.Serialize(loginData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("api/auth/login", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string respuestaJson = await response.Content.ReadAsStringAsync();
                        var authResponse = JsonSerializer.Deserialize<AuthResponse>(respuestaJson, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                        {
                            // Guardar token en el ApiClient
                            _apiClient.SetAuthToken(authResponse.Token);

                            // Guardar información del usuario en la sesión
                            App.Current.Properties["Token"] = authResponse.Token;
                            App.Current.Properties["Username"] = authResponse.Username;
                            App.Current.Properties["Rol"] = authResponse.Rol;

                            MessageBox.Show($"¡Bienvenido, {authResponse.Username}!", 
                                "Inicio de sesión exitoso", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);

                            // Abrir Dashboard
                            DashboardWindow dashboard = new DashboardWindow();
                            dashboard.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Error al procesar la respuesta del servidor.", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        MessageBox.Show("Credenciales incorrectas. Verifique su usuario y contraseña.", 
                            "Error de Autenticación", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"Error del servidor: {response.StatusCode}", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    "No se pudo conectar con el servidor.\n\n" +
                    "Verifique que:\n" +
                    "1. La API esté en ejecución\n" +
                    "2. La URL sea correcta (https://localhost:7269)\n" +
                    "3. No haya problemas de firewall\n\n" +
                    $"Detalle: {ex.Message}", 
                    "Error de Conexión", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show(
                    "La conexión tardó demasiado tiempo.\n" +
                    "Verifique que el servidor esté respondiendo.", 
                    "Tiempo de espera agotado", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnIngresar.IsEnabled = true;
                btnIngresar.Content = "Ingresar";
            }
        }

        // Clase para deserializar la respuesta del login
        private class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Rol { get; set; } = string.Empty;
            public DateTime Expiration { get; set; }
        }
    }
}
