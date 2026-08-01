using Microsoft.EntityFrameworkCore;
using SistemaPOS.API.Models;
using SistemaPOS.GVG.API.Models;

namespace SistemaPOS.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Declaración de las tablas (DbSets)
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<Caja> Cajas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =============== CONFIGURACIÓN DE PRODUCTOS ===============
            modelBuilder.Entity<Producto>(entity =>
            {
                // Índice único en código de barras (búsqueda frecuente)
                entity.HasIndex(p => p.CodigoBarras)
                    .IsUnique()
                    .HasDatabaseName("IX_Productos_CodigoBarras");

                // Índice para búsqueda por categoría
                entity.HasIndex(p => p.Categoria)
                    .HasDatabaseName("IX_Productos_Categoria");

                // Índice para búsqueda por descripción
                entity.HasIndex(p => p.Descripcion)
                    .HasDatabaseName("IX_Productos_Descripcion");

                // Configuración de precisión decimal
                entity.Property(p => p.PrecioCosto)
                    .HasPrecision(18, 2);

                entity.Property(p => p.PrecioVenta)
                    .HasPrecision(18, 2);

                entity.Property(p => p.Stock)
                    .HasPrecision(18, 2);
            });

            // =============== CONFIGURACIÓN DE CLIENTES ===============
            modelBuilder.Entity<Cliente>(entity =>
            {
                // Índice para búsqueda por nombre
                entity.HasIndex(c => c.Nombre)
                    .HasDatabaseName("IX_Clientes_Nombre");

                // Índice filtrado solo clientes activos (consulta más frecuente)
                entity.HasIndex(c => c.Activo)
                    .HasDatabaseName("IX_Clientes_Activo");

                // Índice compuesto para búsquedas activos por nombre
                entity.HasIndex(c => new { c.Activo, c.Nombre })
                    .HasDatabaseName("IX_Clientes_Activo_Nombre");
            });

            // =============== CONFIGURACIÓN DE VENTAS ===============
            modelBuilder.Entity<Venta>(entity =>
            {
                // Índice por fecha de venta (reportes y consultas temporales)
                entity.HasIndex(v => v.FechaVenta)
                    .IsDescending()
                    .HasDatabaseName("IX_Ventas_FechaVenta");

                // Índice por cliente
                entity.HasIndex(v => v.IdCliente)
                    .HasDatabaseName("IX_Ventas_IdCliente");

                // Índice compuesto para ventas no canceladas por fecha
                entity.HasIndex(v => new { v.Cancelada, v.FechaVenta })
                    .IsDescending(false, true)
                    .HasDatabaseName("IX_Ventas_Cancelada_FechaVenta");

                // Índice por usuario para auditoría
                entity.HasIndex(v => v.UsuarioId)
                    .HasDatabaseName("IX_Ventas_UsuarioId");

                // Configuración de precisión decimal
                entity.Property(v => v.Subtotal).HasPrecision(18, 2);
                entity.Property(v => v.Impuesto).HasPrecision(18, 2);
                entity.Property(v => v.Total).HasPrecision(18, 2);
                entity.Property(v => v.Pagado).HasPrecision(18, 2);
                entity.Property(v => v.Cambio).HasPrecision(18, 2);

                // Relación con Cliente (ON DELETE SET NULL)
                entity.HasOne(v => v.Cliente)
                    .WithMany(c => c.Ventas)
                    .HasForeignKey(v => v.IdCliente)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============== CONFIGURACIÓN DE DETALLES DE VENTA ===============
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                // Índice por venta (consulta de detalles)
                entity.HasIndex(dv => dv.IdVenta)
                    .HasDatabaseName("IX_DetalleVentas_IdVenta");

                // Índice por producto (estadísticas de productos más vendidos)
                entity.HasIndex(dv => dv.IdProducto)
                    .HasDatabaseName("IX_DetalleVentas_IdProducto");

                // Configuración de precisión decimal
                entity.Property(dv => dv.Cantidad).HasPrecision(18, 2);
                entity.Property(dv => dv.PrecioUnitario).HasPrecision(18, 2);
                entity.Property(dv => dv.Subtotal).HasPrecision(18, 2);

                // Relación con Venta (ON DELETE CASCADE)
                entity.HasOne(dv => dv.Venta)
                    .WithMany(v => v.Detalles)
                    .HasForeignKey(dv => dv.IdVenta)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con Producto (ON DELETE RESTRICT)
                entity.HasOne(dv => dv.Producto)
                    .WithMany()
                    .HasForeignKey(dv => dv.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============== CONFIGURACIÓN DE CAJAS ===============
            modelBuilder.Entity<Caja>(entity =>
            {
                // Índice por estado para buscar caja activa
                entity.HasIndex(c => c.Estado)
                    .HasDatabaseName("IX_Cajas_Estado");

                // Índice por fecha de apertura
                entity.HasIndex(c => c.FechaApertura)
                    .IsDescending()
                    .HasDatabaseName("IX_Cajas_FechaApertura");

                // Configuración de precisión decimal
                entity.Property(c => c.SaldoInicial).HasPrecision(18, 2);
                entity.Property(c => c.SaldoEfectivo).HasPrecision(18, 2);
                entity.Property(c => c.SaldoFinal).HasPrecision(18, 2);
            });

            // =============== CONFIGURACIÓN DE USUARIOS ===============
            modelBuilder.Entity<Usuario>(entity =>
            {
                // Índice único en username
                entity.HasIndex(u => u.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_Usuarios_Username");

                // Índice para usuarios activos
                entity.HasIndex(u => u.Activo)
                    .HasDatabaseName("IX_Usuarios_Activo");

                // Índice por rol para consultas de autorización
                entity.HasIndex(u => u.Rol)
                    .HasDatabaseName("IX_Usuarios_Rol");
            });

            // =============== DATOS SEMILLA (SEED DATA) ===============
            // Usuario admin por defecto (contraseña: Admin123!)
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    IdUsuario = 1,
                    Username = "admin",
                    // Hash BCrypt de "Admin123!" - workFactor 12
                    PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5NU2U8kqGzP9i",
                    Rol = "Admin",
                    Activo = true
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Optimizaciones de rendimiento
            optionsBuilder
                .EnableSensitiveDataLogging(false) // Desactivar en producción
                .EnableDetailedErrors(false);       // Desactivar en producción
        }
    }
}
