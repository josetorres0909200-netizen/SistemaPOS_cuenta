using System.ComponentModel.DataAnnotations;

namespace SistemaPOS.GVG.API.DTOs
{
    /// <summary>
    /// DTO para crear una nueva venta
    /// </summary>
    public class VentaCreateDTO
    {
        public int? IdCliente { get; set; }

        [Required(ErrorMessage = "Debe incluir al menos un detalle")]
        [MinLength(1, ErrorMessage = "La venta debe incluir al menos un producto")]
        public List<DetalleVentaCreateDTO> Detalles { get; set; } = new();

        [Required]
        [Range(0, 999999.99, ErrorMessage = "El impuesto debe ser positivo")]
        public decimal Impuesto { get; set; } = 0;

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "El monto pagado debe ser mayor a 0")]
        public decimal Pagado { get; set; }

        [StringLength(20)]
        public string TipoPago { get; set; } = "Efectivo";

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public int UsuarioId { get; set; } = 1;
    }

    /// <summary>
    /// DTO para detalle de venta
    /// </summary>
    public class DetalleVentaCreateDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Debe especificar un producto válido")]
        public int IdProducto { get; set; }

        [Required]
        [Range(0.01, 9999.99, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "El precio unitario debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para venta
    /// </summary>
    public class VentaResponseDTO
    {
        public int IdVenta { get; set; }
        public int? IdCliente { get; set; }
        public string? NombreCliente { get; set; }
        public DateTime FechaVenta { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public decimal Cambio { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public int UsuarioId { get; set; }
        public bool Cancelada { get; set; }
        public List<DetalleVentaResponseDTO> Detalles { get; set; } = new();
    }

    /// <summary>
    /// DTO de respuesta para detalle de venta
    /// </summary>
    public class DetalleVentaResponseDTO
    {
        public int IdDetalle { get; set; }
        public int IdProducto { get; set; }
        public string DescripcionProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
