using System.ComponentModel.DataAnnotations;

namespace SistemaPOS.GVG.API.DTOs
{
    /// <summary>
    /// DTO para crear un nuevo producto
    /// </summary>
    public class ProductoCreateDTO
    {
        [Required(ErrorMessage = "El código de barras es obligatorio")]
        [StringLength(50, ErrorMessage = "El código de barras no puede exceder 50 caracteres")]
        public required string CodigoBarras { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
        public required string Descripcion { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [StringLength(50)]
        public required string Categoria { get; set; }

        [Required]
        [StringLength(50)]
        public required string Acabado { get; set; }

        [Required]
        [StringLength(50)]
        public required string Tamanio { get; set; }

        [Required]
        [Range(0, 999999.99, ErrorMessage = "El precio de costo debe estar entre 0 y 999999.99")]
        public decimal PrecioCosto { get; set; }

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "El precio de venta debe ser mayor a 0")]
        public decimal PrecioVenta { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "El stock debe ser positivo")]
        public decimal Stock { get; set; } = 0;
    }

    /// <summary>
    /// DTO para actualizar un producto existente
    /// </summary>
    public class ProductoUpdateDTO : ProductoCreateDTO
    {
        [Required]
        public int IdProducto { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para producto
    /// </summary>
    public class ProductoResponseDTO
    {
        public int IdProducto { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Acabado { get; set; } = string.Empty;
        public string Tamanio { get; set; } = string.Empty;
        public decimal PrecioCosto { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Stock { get; set; }
        public decimal MargenGanancia => PrecioVenta > 0 ? ((PrecioVenta - PrecioCosto) / PrecioVenta) * 100 : 0;
    }
}
