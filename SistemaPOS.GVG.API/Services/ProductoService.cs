using Microsoft.EntityFrameworkCore;
using SistemaPOS.API.Data;
using SistemaPOS.API.Models;
using SistemaPOS.GVG.API.Services.Interfaces;

namespace SistemaPOS.GVG.API.Services
{
    public class ProductoService : IProductoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductoService> _logger;

        public ProductoService(AppDbContext context, ILogger<ProductoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Producto>> GetAllAsync()
        {
            _logger.LogInformation("Consultando todos los productos");
            return await _context.Productos.ToListAsync();
        }

        public async Task<Producto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Consultando producto con ID: {Id}", id);
            return await _context.Productos.FindAsync(id);
        }

        public async Task<Producto?> GetByCodigoBarrasAsync(string codigoBarras)
        {
            _logger.LogInformation("Buscando producto por código de barras: {CodigoBarras}", codigoBarras);
            return await _context.Productos
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras);
        }

        public async Task<Producto> CreateAsync(Producto producto)
        {
            // Validar duplicados
            if (await ExistsByCodigoBarrasAsync(producto.CodigoBarras))
            {
                throw new InvalidOperationException($"Ya existe un producto con el código de barras '{producto.CodigoBarras}'");
            }

            // Validaciones de negocio
            if (producto.PrecioCosto < 0)
                throw new ArgumentException("El precio de costo no puede ser negativo");

            if (producto.PrecioVenta < producto.PrecioCosto)
                throw new ArgumentException("El precio de venta debe ser mayor o igual al precio de costo");

            if (producto.Stock < 0)
                throw new ArgumentException("El stock no puede ser negativo");

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Producto creado exitosamente: {Id} - {Descripcion}", producto.IdProducto, producto.Descripcion);
            return producto;
        }

        public async Task<Producto> UpdateAsync(Producto producto)
        {
            var existente = await _context.Productos.FindAsync(producto.IdProducto);
            if (existente == null)
            {
                throw new InvalidOperationException($"Producto con ID {producto.IdProducto} no encontrado");
            }

            // Verificar código de barras duplicado (excluyendo el actual)
            if (await ExistsByCodigoBarrasAsync(producto.CodigoBarras, producto.IdProducto))
            {
                throw new InvalidOperationException($"Ya existe otro producto con el código de barras '{producto.CodigoBarras}'");
            }

            // Validaciones de negocio
            if (producto.PrecioCosto < 0)
                throw new ArgumentException("El precio de costo no puede ser negativo");

            if (producto.PrecioVenta < producto.PrecioCosto)
                throw new ArgumentException("El precio de venta debe ser mayor o igual al precio de costo");

            _context.Entry(existente).CurrentValues.SetValues(producto);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Producto actualizado exitosamente: {Id} - {Descripcion}", producto.IdProducto, producto.Descripcion);
            return producto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {id} no encontrado");
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Producto eliminado exitosamente: {Id} - {Descripcion}", id, producto.Descripcion);
            return true;
        }

        public async Task<bool> ExistsByCodigoBarrasAsync(string codigoBarras, int? excludeId = null)
        {
            var query = _context.Productos.Where(p => p.CodigoBarras == codigoBarras);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.IdProducto != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Producto>> SearchAsync(string searchTerm)
        {
            _logger.LogInformation("Buscando productos con término: {SearchTerm}", searchTerm);

            return await _context.Productos
                .Where(p => p.Descripcion.Contains(searchTerm) ||
                           p.CodigoBarras.Contains(searchTerm) ||
                           p.Categoria.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<bool> UpdateStockAsync(int idProducto, decimal cantidad)
        {
            var producto = await _context.Productos.FindAsync(idProducto);
            if (producto == null)
            {
                throw new InvalidOperationException($"Producto con ID {idProducto} no encontrado");
            }

            var nuevoStock = producto.Stock + cantidad;
            if (nuevoStock < 0)
            {
                throw new InvalidOperationException($"Stock insuficiente. Disponible: {producto.Stock}, Solicitado: {Math.Abs(cantidad)}");
            }

            producto.Stock = nuevoStock;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Stock actualizado para producto {Id}: {StockAnterior} -> {StockNuevo}", 
                idProducto, producto.Stock - cantidad, producto.Stock);

            return true;
        }
    }
}
