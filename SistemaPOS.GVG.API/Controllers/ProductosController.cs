using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPOS.API.Models;
using SistemaPOS.GVG.API.Services.Interfaces;

namespace SistemaPOS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación JWT
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(IProductoService productoService, ILogger<ProductosController> logger)
        {
            _productoService = productoService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el catálogo completo de productos
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Producto>>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<Producto>>>> GetProductos()
        {
            var productos = await _productoService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<Producto>>.SuccessResponse(productos,
                $"Se encontraron {productos.Count()} productos"));
        }

        /// <summary>
        /// Obtiene un producto por su ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Producto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<Producto>>> GetProducto(int id)
        {
            var producto = await _productoService.GetByIdAsync(id);

            if (producto == null)
                return NotFound(ApiResponse<object>.ErrorResponse($"Producto con ID {id} no encontrado"));

            return Ok(ApiResponse<Producto>.SuccessResponse(producto, "Producto encontrado"));
        }

        /// <summary>
        /// Busca un producto por código de barras
        /// </summary>
        [HttpGet("buscar/codigo/{codigoBarras}")]
        [ProducesResponseType(typeof(ApiResponse<Producto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<Producto>>> GetProductoPorCodigoBarras(string codigoBarras)
        {
            var producto = await _productoService.GetByCodigoBarrasAsync(codigoBarras);

            if (producto == null)
                return NotFound(ApiResponse<object>.ErrorResponse(
                    $"Producto con código de barras '{codigoBarras}' no encontrado"));

            return Ok(ApiResponse<Producto>.SuccessResponse(producto, "Producto encontrado"));
        }

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Gerente")]
        [ProducesResponseType(typeof(ApiResponse<Producto>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<Producto>>> PostProducto(Producto producto)
        {
            try
            {
                if (producto == null)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El producto no puede ser nulo"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse("Datos inválidos", errors));
                }

                // Validar campos obligatorios
                if (string.IsNullOrWhiteSpace(producto.Descripcion))
                    return BadRequest(ApiResponse<object>.ErrorResponse("La descripción del producto es requerida"));

                if (string.IsNullOrWhiteSpace(producto.CodigoBarras))
                    return BadRequest(ApiResponse<object>.ErrorResponse("El código de barras es requerido"));

                var nuevoProducto = await _productoService.CreateAsync(producto);

                return CreatedAtAction(nameof(GetProducto), new { id = nuevoProducto.IdProducto },
                    ApiResponse<Producto>.SuccessResponse(nuevoProducto, "Producto creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error al crear producto: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al crear producto: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear producto");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al crear el producto"));
            }
        }

        /// <summary>
        /// Actualiza un producto existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Gerente")]
        [ProducesResponseType(typeof(ApiResponse<Producto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<Producto>>> PutProducto(int id, Producto producto)
        {
            try
            {
                if (producto == null)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El producto no puede ser nulo"));

                if (id != producto.IdProducto)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El ID en la URL no coincide con el producto"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse("Datos inválidos", errors));
                }

                // Validar campos obligatorios
                if (string.IsNullOrWhiteSpace(producto.Descripcion))
                    return BadRequest(ApiResponse<object>.ErrorResponse("La descripción del producto es requerida"));

                if (string.IsNullOrWhiteSpace(producto.CodigoBarras))
                    return BadRequest(ApiResponse<object>.ErrorResponse("El código de barras es requerido"));

                var actualizado = await _productoService.UpdateAsync(producto);
                return Ok(ApiResponse<Producto>.SuccessResponse(actualizado, "Producto actualizado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Producto no encontrado: {Message}", ex.Message);
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al actualizar producto: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar producto");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al actualizar el producto"));
            }
        }

        /// <summary>
        /// Elimina un producto
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProducto(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El ID del producto debe ser válido"));

                await _productoService.DeleteAsync(id);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Producto eliminado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Producto no encontrado para eliminar: {Message}", ex.Message);
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar producto");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al eliminar el producto"));
            }
        }

        /// <summary>
        /// Busca productos por término de búsqueda
        /// </summary>
        [HttpGet("buscar")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Producto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<Producto>>>> BuscarProductos([FromQuery] string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                    return BadRequest(ApiResponse<object>.ErrorResponse("El término de búsqueda es requerido"));

                if (termino.Length < 2)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El término de búsqueda debe tener al menos 2 caracteres"));

                var productos = await _productoService.SearchAsync(termino);
                return Ok(ApiResponse<IEnumerable<Producto>>.SuccessResponse(productos,
                    $"Se encontraron {productos.Count()} productos"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al buscar productos"));
            }
        }
    }
}