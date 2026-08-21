using System.ComponentModel.DataAnnotations;

namespace SistemaPOS.GVG.API.Models
{
    /// <summary>
    /// DTO para respuesta de autenticación exitosa
    /// </summary>
    public class AuthResponseDTO
    {
        /// <summary>
        /// Token JWT para autenticación en las próximas solicitudes
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token de refresco para renovar el JWT sin volver a autenticarse
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de usuario autenticado
        /// </summary>
        [Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Rol del usuario (Admin, Gerente, Vendedor, etc.)
        /// </summary>
        [Required]
        public string Rol { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de expiración del token JWT
        /// </summary>
        [Required]
        public DateTime Expiration { get; set; }
    }
}
