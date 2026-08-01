using Microsoft.EntityFrameworkCore;
using SistemaPOS.API.Data;
using SistemaPOS.API.Models;
using SistemaPOS.GVG.API.Services.Interfaces;

namespace SistemaPOS.GVG.API.Services
{
    public class VentaService : IVentaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VentaService> _logger;

        public VentaService(AppDbContext context, ILogger<VentaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Venta>> GetAllAsync(int? dias = null)
        {
            _logger.LogInformation("Consultando ventas (días: {Dias})", dias ?? 0);

            IQueryable<Venta> query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto);

            if (dias.HasValue)
            {
                var fechaLimite = DateTime.Now.AddDays(-dias.Value);
                query = query.Where(v => v.FechaVenta >= fechaLimite);
            }

            return await query
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();
        }

        public async Task<Venta?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Consultando venta con ID: {Id}", id);

            return await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.IdVenta == id);
        }

        public async Task<Venta> CreateAsync(Venta venta)
        {
            // Usar transacción para garantizar consistencia
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validaciones de negocio
                if (!venta.Detalles.Any())
                    throw new ArgumentException("La venta debe incluir al menos un detalle");

                if (venta.Pagado < 0)
                    throw new ArgumentException("El monto pagado no puede ser negativo");

                // Validar y actualizar stock de forma atómica
                foreach (var detalle in venta.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.IdProducto);

                    if (producto == null)
                        throw new InvalidOperationException($"Producto con ID {detalle.IdProducto} no encontrado");

                    if (producto.Stock < detalle.Cantidad)
                        throw new InvalidOperationException(
                            $"Stock insuficiente para '{producto.Descripcion}'. Disponible: {producto.Stock}, Solicitado: {detalle.Cantidad}");

                    // Actualizar stock
                    producto.Stock -= detalle.Cantidad;

                    // Calcular subtotal del detalle
                    detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                }

                // Calcular totales de la venta
                venta.Subtotal = venta.Detalles.Sum(d => d.Subtotal);
                venta.Total = venta.Subtotal + venta.Impuesto;
                venta.Cambio = venta.Pagado - venta.Total;

                if (venta.Cambio < 0)
                    throw new ArgumentException("El monto pagado es insuficiente para cubrir el total de la venta");

                venta.FechaVenta = DateTime.Now;
                venta.Cancelada = false;

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Venta creada exitosamente: {Id} - Total: {Total:C} - Cliente: {Cliente}", 
                    venta.IdVenta, 
                    venta.Total, 
                    venta.IdCliente?.ToString() ?? "Sin cliente");

                return venta;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear venta. Transacción revertida");
                throw;
            }
        }

        public async Task<bool> CancelarVentaAsync(int id, int usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var venta = await _context.Ventas
                    .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(v => v.IdVenta == id);

                if (venta == null)
                    throw new InvalidOperationException($"Venta con ID {id} no encontrada");

                if (venta.Cancelada)
                    throw new InvalidOperationException("La venta ya está cancelada");

                // Revertir el stock
                foreach (var detalle in venta.Detalles)
                {
                    if (detalle.Producto != null)
                    {
                        detalle.Producto.Stock += detalle.Cantidad;
                    }
                }

                venta.Cancelada = true;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Venta cancelada: {Id} - Usuario: {UsuarioId}", id, usuarioId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al cancelar venta {Id}. Transacción revertida", id);
                throw;
            }
        }

        public async Task<object> GetResumenVentasAsync(int? dias = 30)
        {
            _logger.LogInformation("Generando resumen de ventas (días: {Dias})", dias ?? 30);

            var fechaLimite = DateTime.Now.AddDays(-(dias ?? 30));
            var ventas = await _context.Ventas
                .Where(v => v.FechaVenta >= fechaLimite && !v.Cancelada)
                .ToListAsync();

            var resumen = new
            {
                TotalVentas = ventas.Count,
                MontoTotal = ventas.Sum(v => v.Total),
                MontoPromedio = ventas.Any() ? ventas.Average(v => v.Total) : 0,
                VentasPorDia = ventas
                    .GroupBy(v => v.FechaVenta.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Cantidad = g.Count(),
                        Monto = g.Sum(v => v.Total)
                    })
                    .OrderByDescending(x => x.Fecha)
                    .ToList(),
                VentasPorTipoPago = ventas
                    .GroupBy(v => v.TipoPago)
                    .Select(g => new
                    {
                        TipoPago = g.Key,
                        Cantidad = g.Count(),
                        Monto = g.Sum(v => v.Total)
                    })
                    .ToList()
            };

            return resumen;
        }

        public async Task<IEnumerable<Venta>> GetVentasPorClienteAsync(int idCliente)
        {
            _logger.LogInformation("Consultando ventas del cliente: {IdCliente}", idCliente);

            return await _context.Ventas
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(v => v.IdCliente == idCliente && !v.Cancelada)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalVentasDelDiaAsync()
        {
            var hoy = DateTime.Today;
            var totalDelDia = await _context.Ventas
                .Where(v => v.FechaVenta.Date == hoy && !v.Cancelada)
                .SumAsync(v => v.Total);

            _logger.LogInformation("Total de ventas del día: {Total:C}", totalDelDia);
            return totalDelDia;
        }
    }
}
