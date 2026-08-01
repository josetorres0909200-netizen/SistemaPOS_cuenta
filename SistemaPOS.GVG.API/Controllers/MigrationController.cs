using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPOS.GVG.API.Utilities;

namespace SistemaPOS.GVG.API.Controllers
{
    /// <summary>
    /// Controller temporal para ejecutar migraciones de BD
    /// ⚠️ ELIMINAR EN PRODUCCIÓN DESPUÉS DE USAR
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class MigrationController : ControllerBase
    {
        private readonly PasswordMigrationUtility _migrationUtility;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(PasswordMigrationUtility migrationUtility, ILogger<MigrationController> logger)
        {
            _migrationUtility = migrationUtility;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta la migración de contraseñas a BCrypt
        /// ⚠️ EJECUTAR UNA SOLA VEZ
        /// </summary>
        [HttpPost("migrate-passwords")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> MigratePasswords()
        {
            _logger.LogWarning("Iniciando migración de contraseñas manualmente por usuario: {User}", 
                User.Identity?.Name ?? "Unknown");

            // Verificar si es necesario migrar
            var needsMigration = await _migrationUtility.NeedsMigrationAsync();

            if (!needsMigration)
            {
                return Ok(new
                {
                    mensaje = "✓ No hay contraseñas pendientes de migrar. Todas las contraseñas ya están hasheadas.",
                    timestamp = DateTime.Now
                });
            }

            // Ejecutar migración
            var result = await _migrationUtility.MigratePasswordsAsync();

            if (result.Success)
            {
                return Ok(new
                {
                    mensaje = "✓ Migración completada exitosamente",
                    resumen = result.GetSummary(),
                    totalProcesados = result.TotalProcessed,
                    migrados = result.MigratedUsers.Count(u => u.Success),
                    yaHasheados = result.AlreadyHashedCount,
                    errores = result.MigratedUsers.Count(u => !u.Success),
                    timestamp = result.CompletedAt
                });
            }
            else
            {
                return BadRequest(new
                {
                    mensaje = "✗ La migración falló",
                    error = result.GlobalError,
                    timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Verifica si hay contraseñas pendientes de migrar
        /// </summary>
        [HttpGet("check-migration-needed")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CheckMigrationNeeded()
        {
            var needsMigration = await _migrationUtility.NeedsMigrationAsync();

            return Ok(new
            {
                needsMigration,
                mensaje = needsMigration 
                    ? "⚠️ Hay contraseñas pendientes de migrar a BCrypt" 
                    : "✓ Todas las contraseñas están hasheadas",
                timestamp = DateTime.Now
            });
        }
    }
}
