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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Datos inválidos", errors));
            }

            var nuevoProducto = await _productoService.CreateAsync(producto);

            return CreatedAtAction(nameof(GetProducto), new { id = nuevoProducto.IdProducto },
                ApiResponse<Producto>.SuccessResponse(nuevoProducto, "Producto creado exitosamente"));
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
            if (id != producto.IdProducto)
                return BadRequest(ApiResponse<object>.ErrorResponse("El ID en la URL no coincide con el producto"));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Datos inválidos", errors));
            }

            var actualizado = await _productoService.UpdateAsync(producto);
            return Ok(ApiResponse<Producto>.SuccessResponse(actualizado, "Producto actualizado exitosamente"));
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
            await _productoService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Producto eliminado exitosamente"));
        }

        /// <summary>
        /// Busca productos por término de búsqueda
        /// </summary>
        [HttpGet("buscar")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Producto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<Producto>>>> BuscarProductos([FromQuery] string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return BadRequest(ApiResponse<object>.ErrorResponse("El término de búsqueda es requerido"));

            var productos = await _productoService.SearchAsync(termino);
            return Ok(ApiResponse<IEnumerable<Producto>>.SuccessResponse(productos,
                $"Se encontraron {productos.Count()} productos"));
        }
    }
}