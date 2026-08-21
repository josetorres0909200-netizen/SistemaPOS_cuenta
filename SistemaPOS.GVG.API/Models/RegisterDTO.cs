using System.ComponentModel.DataAnnotations;

namespace SistemaPOS.GVG.API.Models
{
    /// <summary>
    /// DTO para registro de nuevos usuarios
    /// </summary>
    public class RegisterDTO
    {
        /// <summary>
        /// Nombre de usuario único
        /// </summary>
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres")]
        public required string Username { get; set; }

        /// <summary>
        /// Contraseña del usuario (se almacenará hasheada)
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener mínimo 6 caracteres")]
        public required string Password { get; set; }

        /// <summary>
        /// Rol del usuario (Admin, Gerente, Vendedor)
        /// Por defecto es "Vendedor"
        /// </summary>
        [StringLength(50, ErrorMessage = "El rol no puede exceder 50 caracteres")]
        public string? Rol { get; set; } = "Vendedor";
    }
}
