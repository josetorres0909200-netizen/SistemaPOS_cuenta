using SistemaPOS.API.Models;

namespace SistemaPOS.GVG.API.Services.Interfaces
{
    public interface IVentaService
    {
        Task<IEnumerable<Venta>> GetAllAsync(int? dias = null);
        Task<Venta?> GetByIdAsync(int id);
        Task<Venta> CreateAsync(Venta venta);
        Task<bool> CancelarVentaAsync(int id, int usuarioId);
        Task<object> GetResumenVentasAsync(int? dias = 30);
        Task<IEnumerable<Venta>> GetVentasPorClienteAsync(int idCliente);
        Task<decimal> GetTotalVentasDelDiaAsync();
    }
}
