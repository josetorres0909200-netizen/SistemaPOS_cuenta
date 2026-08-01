using SistemaPOS.GVG.API.Models;

namespace SistemaPOS.GVG.API.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario y genera un token JWT
        /// </summary>
        Task<AuthResponseDTO?> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Registra un nuevo usuario con contraseña hasheada
        /// </summary>
        Task<Usuario> RegisterUserAsync(string username, string password, string rol);

        /// <summary>
        /// Verifica un token JWT
        /// </summary>
        bool ValidateToken(string token);

        /// <summary>
        /// Hashea una contraseña de forma segura
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifica si una contraseña coincide con el hash
        /// </summary>
        bool VerifyPassword(string password, string hash);
    }

    public class AuthResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
