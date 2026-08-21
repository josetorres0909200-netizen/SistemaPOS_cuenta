using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPOS.API.Models;
using SistemaPOS.GVG.API.Services.Interfaces;

namespace SistemaPOS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VentasController : ControllerBase
    {
        private readonly IVentaService _ventaService;
        private readonly ILogger<VentasController> _logger;

        public VentasController(IVentaService ventaService, ILogger<VentasController> logger)
        {
            _ventaService = ventaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las ventas con filtro opcional por días
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Venta>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<Venta>>>> GetVentas([FromQuery] int? dias = null)
        {
            try
            {
                if (dias.HasValue && dias.Value < 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El número de días debe ser positivo"));

                var ventas = await _ventaService.GetAllAsync(dias);
                return Ok(ApiResponse<IEnumerable<Venta>>.SuccessResponse(ventas,
                    $"Se encontraron {ventas.Count()} ventas"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al obtener las ventas"));
            }
        }

        /// <summary>
        /// Obtiene una venta por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Venta>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<Venta>>> GetVenta(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El ID de la venta debe ser válido"));

                var venta = await _ventaService.GetByIdAsync(id);

                if (venta == null)
                    return NotFound(ApiResponse<object>.ErrorResponse($"Venta con ID {id} no encontrada"));

                return Ok(ApiResponse<Venta>.SuccessResponse(venta, "Venta encontrada"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener venta");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al obtener la venta"));
            }
        }

        /// <summary>
        /// Registra una nueva venta con validación de stock y transacciones atómicas
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Venta>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<Venta>>> PostVenta(Venta venta)
        {
            try
            {
                if (venta == null)
                    return BadRequest(ApiResponse<object>.ErrorResponse("La venta no puede ser nula"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse("Datos inválidos", errors));
                }

                if (venta.Detalles == null || !venta.Detalles.Any())
                    return BadRequest(ApiResponse<object>.ErrorResponse("La venta debe contener al menos un detalle"));

                var nuevaVenta = await _ventaService.CreateAsync(venta);

                _logger.LogInformation("Venta creada: {IdVenta} - Total: {Total}", nuevaVenta.IdVenta, nuevaVenta.Total);
                return CreatedAtAction(nameof(GetVenta), new { id = nuevaVenta.IdVenta },
                    ApiResponse<Venta>.SuccessResponse(nuevaVenta, "Venta registrada exitosamente"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al crear venta: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error operacional al crear venta: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear venta");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al registrar la venta"));
            }
        }

        /// <summary>
        /// Cancela una venta y revierte el stock
        /// </summary>
        [HttpPut("{id}/cancelar")]
        [Authorize(Roles = "Admin,Gerente")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<object>>> CancelarVenta(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El ID de la venta debe ser válido"));

                var usuarioId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
                await _ventaService.CancelarVentaAsync(id, usuarioId);

                _logger.LogInformation("Venta cancelada: {IdVenta} por usuario: {UsuarioId}", id, usuarioId);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Venta cancelada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error al cancelar venta: {Message}", ex.Message);
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cancelar venta");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al cancelar la venta"));
            }
        }

        /// <summary>
        /// Obtiene resumen de ventas por período
        /// </summary>
        [HttpGet("reporte/resumen")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetResumenVentas([FromQuery] int? dias = 30)
        {
            try
            {
                if (dias.HasValue && dias.Value < 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El número de días debe ser positivo"));

                var resumen = await _ventaService.GetResumenVentasAsync(dias);
                return Ok(ApiResponse<object>.SuccessResponse(resumen, "Resumen de ventas generado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen de ventas");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al generar el resumen"));
            }
        }

        /// <summary>
        /// Obtiene ventas de un cliente específico
        /// </summary>
        [HttpGet("cliente/{idCliente}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Venta>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<Venta>>>> GetVentasPorCliente(int idCliente)
        {
            try
            {
                if (idCliente <= 0)
                    return BadRequest(ApiResponse<object>.ErrorResponse("El ID del cliente debe ser válido"));

                var ventas = await _ventaService.GetVentasPorClienteAsync(idCliente);
                return Ok(ApiResponse<IEnumerable<Venta>>.SuccessResponse(ventas,
                    $"Se encontraron {ventas.Count()} ventas para el cliente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas del cliente");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al obtener las ventas del cliente"));
            }
        }

        /// <summary>
        /// Obtiene el total de ventas del día actual
        /// </summary>
        [HttpGet("total-del-dia")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), 200)]
        public async Task<ActionResult<ApiResponse<decimal>>> GetTotalDelDia()
        {
            try
            {
                var total = await _ventaService.GetTotalVentasDelDiaAsync();
                return Ok(ApiResponse<decimal>.SuccessResponse(total, $"Total de ventas del día: {total:C}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener total del día");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error al obtener el total del día"));
            }
        }
    }
}
