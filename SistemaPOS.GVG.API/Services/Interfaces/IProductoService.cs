using SistemaPOS.API.Models;

namespace SistemaPOS.GVG.API.Services.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<Producto>> GetAllAsync();
        Task<Producto?> GetByIdAsync(int id);
        Task<Producto?> GetByCodigoBarrasAsync(string codigoBarras);
        Task<Producto> CreateAsync(Producto producto);
        Task<Producto> UpdateAsync(Producto producto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByCodigoBarrasAsync(string codigoBarras, int? excludeId = null);
        Task<IEnumerable<Producto>> SearchAsync(string searchTerm);
        Task<bool> UpdateStockAsync(int idProducto, decimal cantidad);
    }
}
