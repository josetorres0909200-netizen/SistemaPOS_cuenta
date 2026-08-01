using System.ComponentModel.DataAnnotations;

namespace SistemaPOS.GVG.API.DTOs
{
    /// <summary>
    /// DTO para crear un cliente
    /// </summary>
    public class ClienteCreateDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        public required string Nombre { get; set; }

        [StringLength(15)]
        [Phone(ErrorMessage = "El teléfono no es válido")]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "El email no es válido")]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un cliente
    /// </summary>
    public class ClienteUpdateDTO : ClienteCreateDTO
    {
        [Required]
        public int IdCliente { get; set; }

        public bool Activo { get; set; } = true;
    }

    /// <summary>
    /// DTO de respuesta para cliente
    /// </summary>
    public class ClienteResponseDTO
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int TotalCompras { get; set; }
        public decimal MontoTotalComprado { get; set; }
    }
}
