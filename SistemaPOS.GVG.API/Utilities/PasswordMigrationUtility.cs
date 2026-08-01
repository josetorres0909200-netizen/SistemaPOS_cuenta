using Microsoft.EntityFrameworkCore;
using SistemaPOS.API.Data;
using BCrypt.Net;

namespace SistemaPOS.GVG.API.Utilities
{
    /// <summary>
    /// Utilidad para migrar contraseñas en texto plano a hashes BCrypt
    /// ⚠️ EJECUTAR UNA SOLA VEZ DESPUÉS DE ACTUALIZAR LA BD
    /// </summary>
    public class PasswordMigrationUtility
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PasswordMigrationUtility> _logger;

        public PasswordMigrationUtility(AppDbContext context, ILogger<PasswordMigrationUtility> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Migra contraseñas en texto plano a BCrypt
        /// </summary>
        public async Task<MigrationResult> MigratePasswordsAsync()
        {
            _logger.LogInformation("Iniciando migración de contraseñas...");

            var result = new MigrationResult();

            try
            {
                // Obtener todos los usuarios
                var usuarios = await _context.Usuarios.ToListAsync();

                foreach (var usuario in usuarios)
                {
                    // Detectar si la contraseña NO es un hash BCrypt
                    if (!usuario.PasswordHash.StartsWith("$2a$") && 
                        !usuario.PasswordHash.StartsWith("$2b$") && 
                        !usuario.PasswordHash.StartsWith("$2y$"))
                    {
                        _logger.LogInformation("Migrando contraseña de usuario: {Username}", usuario.Username);

                        try
                        {
                            // Guardar contraseña original (para verificación)
                            var passwordOriginal = usuario.PasswordHash;

                            // Hashear la contraseña con BCrypt
                            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordOriginal, 12);

                            result.MigratedUsers.Add(new MigratedUser
                            {
                                Username = usuario.Username,
                                OriginalPassword = passwordOriginal, // Solo para log, no guardar
                                Success = true
                            });

                            _logger.LogInformation("✓ Usuario {Username} migrado exitosamente", usuario.Username);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error al migrar usuario: {Username}", usuario.Username);
                            result.MigratedUsers.Add(new MigratedUser
                            {
                                Username = usuario.Username,
                                Success = false,
                                Error = ex.Message
                            });
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Usuario {Username} ya tiene contraseña hasheada. Omitiendo.", usuario.Username);
                        result.AlreadyHashedCount++;
                    }
                }

                // Guardar cambios
                if (result.MigratedUsers.Any(u => u.Success))
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✓ Cambios guardados en la base de datos");
                    result.Success = true;
                }

                result.TotalProcessed = usuarios.Count;
                result.CompletedAt = DateTime.Now;

                _logger.LogInformation(
                    "Migración completada. Total: {Total}, Migrados: {Migrated}, Ya hasheados: {AlreadyHashed}, Errores: {Errors}",
                    result.TotalProcessed,
                    result.MigratedUsers.Count(u => u.Success),
                    result.AlreadyHashedCount,
                    result.MigratedUsers.Count(u => !u.Success)
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante la migración");
                result.Success = false;
                result.GlobalError = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Verifica si hay contraseñas pendientes de migrar
        /// </summary>
        public async Task<bool> NeedsMigrationAsync()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return usuarios.Any(u => 
                !u.PasswordHash.StartsWith("$2a$") && 
                !u.PasswordHash.StartsWith("$2b$") && 
                !u.PasswordHash.StartsWith("$2y$"));
        }
    }

    public class MigrationResult
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int AlreadyHashedCount { get; set; }
        public List<MigratedUser> MigratedUsers { get; set; } = new();
        public string? GlobalError { get; set; }
        public DateTime CompletedAt { get; set; }

        public string GetSummary()
        {
            var successCount = MigratedUsers.Count(u => u.Success);
            var failedCount = MigratedUsers.Count(u => !u.Success);

            return $@"
╔════════════════════════════════════════════════════════╗
║         MIGRACIÓN DE CONTRASEÑAS - RESUMEN            ║
╠════════════════════════════════════════════════════════╣
║ Total de usuarios procesados: {TotalProcessed,19} ║
║ Contraseñas migradas:         {successCount,19} ║
║ Ya estaban hasheadas:          {AlreadyHashedCount,19} ║
║ Errores:                       {failedCount,19} ║
║                                                        ║
║ Estado: {(Success ? "✓ ÉXITO" : "✗ FALLIDO"),40} ║
║ Fecha: {CompletedAt:dd/MM/yyyy HH:mm:ss,41} ║
╚════════════════════════════════════════════════════════╝

⚠️  IMPORTANTE:
1. Notificar a los usuarios que sus contraseñas fueron migradas
2. Las contraseñas siguen siendo las mismas (ahora hasheadas)
3. Eliminar este script después de ejecutar la migración
4. Verificar que todos los usuarios puedan iniciar sesión
";
        }
    }

    public class MigratedUser
    {
        public string Username { get; set; } = string.Empty;
        public string? OriginalPassword { get; set; } // Solo para verificación
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
