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
            var ventas = await _ventaService.GetAllAsync(dias);
            return Ok(ApiResponse<IEnumerable<Venta>>.SuccessResponse(ventas,
                $"Se encontraron {ventas.Count()} ventas"));
        }

        /// <summary>
        /// Obtiene una venta por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Venta>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<Venta>>> GetVenta(int id)
        {
            var venta = await _ventaService.GetByIdAsync(id);

            if (venta == null)
                return NotFound(ApiResponse<object>.ErrorResponse($"Venta con ID {id} no encontrada"));

            return Ok(ApiResponse<Venta>.SuccessResponse(venta, "Venta encontrada"));
        }

        /// <summary>
        /// Registra una nueva venta con validación de stock y transacciones atómicas
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Venta>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<Venta>>> PostVenta(Venta venta)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Datos inválidos", errors));
            }

            var nuevaVenta = await _ventaService.CreateAsync(venta);

            _logger.LogInformation("Venta creada: {IdVenta} - Total: {Total}", nuevaVenta.IdVenta, nuevaVenta.Total);
            return CreatedAtAction(nameof(GetVenta), new { id = nuevaVenta.IdVenta },
                ApiResponse<Venta>.SuccessResponse(nuevaVenta, "Venta registrada exitosamente"));
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
            var usuarioId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
            await _ventaService.CancelarVentaAsync(id, usuarioId);

            return Ok(ApiResponse<object>.SuccessResponse(null, "Venta cancelada exitosamente"));
        }

        /// <summary>
        /// Obtiene resumen de ventas por período
        /// </summary>
        [HttpGet("reporte/resumen")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> GetResumenVentas([FromQuery] int? dias = 30)
        {
            var resumen = await _ventaService.GetResumenVentasAsync(dias);
            return Ok(ApiResponse<object>.SuccessResponse(resumen, "Resumen de ventas generado"));
        }

        /// <summary>
        /// Obtiene ventas de un cliente específico
        /// </summary>
        [HttpGet("cliente/{idCliente}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<Venta>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<Venta>>>> GetVentasPorCliente(int idCliente)
        {
            var ventas = await _ventaService.GetVentasPorClienteAsync(idCliente);
            return Ok(ApiResponse<IEnumerable<Venta>>.SuccessResponse(ventas,
                $"Se encontraron {ventas.Count()} ventas para el cliente"));
        }

        /// <summary>
        /// Obtiene el total de ventas del día actual
        /// </summary>
        [HttpGet("total-del-dia")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), 200)]
        public async Task<ActionResult<ApiResponse<decimal>>> GetTotalDelDia()
        {
            var total = await _ventaService.GetTotalVentasDelDiaAsync();
            return Ok(ApiResponse<decimal>.SuccessResponse(total, $"Total de ventas del día: {total:C}"));
        }
    }
}
