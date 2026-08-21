using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaPOS.API.Data;
using SistemaPOS.GVG.API.Models;
using SistemaPOS.GVG.API.Services.Interfaces;

namespace SistemaPOS.GVG.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, AppDbContext context)
        {
            _authService = authService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Autentica un usuario y retorna un token JWT
        /// </summary>
        /// <param name="request">Credenciales de usuario</param>
        /// <returns>Token JWT y datos del usuario</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDTO), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { mensaje = "Usuario y contraseña son requeridos" });
            }

            var authResponse = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (authResponse == null)
            {
                _logger.LogWarning("Intento de login fallido para usuario: {Username}", request.Username);
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
            }

            _logger.LogInformation("Usuario autenticado exitosamente: {Username}", request.Username);

            return Ok(authResponse);
        }

        /// <summary>
        /// Registra un nuevo usuario (solo Admin)
        /// </summary>
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Usuario), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _authService.RegisterUserAsync(
                request.Username,
                request.Password,
                request.Rol ?? "Vendedor");

            _logger.LogInformation("Usuario registrado: {Username} - Rol: {Rol}", usuario.Username, usuario.Rol);

            return CreatedAtAction(nameof(Login), new { username = usuario.Username }, new
            {
                usuario.IdUsuario,
                usuario.Username,
                usuario.Rol
            });
        }

        /// <summary>
        /// Valida si un token JWT es válido
        /// </summary>
        [HttpPost("validate-token")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public IActionResult ValidateToken([FromBody] TokenValidationRequest request)
        {
            var isValid = _authService.ValidateToken(request.Token);

            if (!isValid)
            {
                return Unauthorized(new { mensaje = "Token inválido o expirado" });
            }

            return Ok(new { mensaje = "Token válido" });
        }

        /// <summary>
        /// ⚠️ ENDPOINT TEMPORAL DE DIAGNÓSTICO - Verificar conexión a BD
        /// </summary>
        [HttpGet("db-info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDatabaseInfo()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var canConnect = await _context.Database.CanConnectAsync();
                var databaseName = _context.Database.GetDbConnection().Database;

                var usuarios = await _context.Usuarios.ToListAsync();

                return Ok(new
                {
                    conexionExitosa = canConnect,
                    baseDeDatos = databaseName,
                    connectionString = connectionString?.Replace("Password=", "Password=****"),
                    totalUsuarios = usuarios.Count,
                    usuarios = usuarios.Select(u => new
                    {
                        u.IdUsuario,
                        u.Username,
                        u.Rol,
                        u.Activo,
                        HashLength = u.PasswordHash.Length,
                        HashStart = u.PasswordHash.Substring(0, Math.Min(50, u.PasswordHash.Length))
                    })
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// ⚠️ ENDPOINT TEMPORAL DE DIAGNÓSTICO - ELIMINAR EN PRODUCCIÓN
        /// Prueba directamente BCrypt.Verify contra el hash almacenado
        /// </summary>
        [HttpPost("test-hash")]
        [AllowAnonymous]
        public async Task<IActionResult> TestHash([FromBody] TestHashRequest request)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (usuario == null)
                {
                    return Ok(new
                    {
                        resultado = "USUARIO_NO_ENCONTRADO",
                        username = request.Username,
                        usuariosDisponibles = await _context.Usuarios.Select(u => u.Username).ToListAsync()
                    });
                }

                // Información del usuario
                var info = new
                {
                    resultado = "USUARIO_ENCONTRADO",
                    username = usuario.Username,
                    rol = usuario.Rol,
                    activo = usuario.Activo,
                    hashLength = usuario.PasswordHash.Length,
                    hashStart = usuario.PasswordHash.Substring(0, Math.Min(30, usuario.PasswordHash.Length)),
                    esSHA256Format = usuario.PasswordHash.Length == 44 && usuario.PasswordHash.EndsWith("=")
                };

                // Probar validación SHA256
                bool hashVerifyResult = _authService.VerifyPassword(request.Password, usuario.PasswordHash);

                return Ok(new
                {
                    info.resultado,
                    info.username,
                    info.rol,
                    info.activo,
                    info.hashLength,
                    info.hashStart,
                    info.esSHA256Format,
                    passwordIntroducida = request.Password,
                    hashVerify = hashVerifyResult ? "✅ COINCIDE" : "❌ NO COINCIDE",
                    diagnostico = hashVerifyResult ? 
                        "Las credenciales son correctas. El problema está en otro lugar." :
                        "La contraseña no coincide con el hash almacenado. Verifica el hash en la BD."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    resultado = "ERROR",
                    mensaje = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// ⚠️ ENDPOINT TEMPORAL - Genera un hash SHA256 para una contraseña
        /// </summary>
        [HttpPost("generate-hash")]
        [AllowAnonymous]
        public IActionResult GenerateHash([FromBody] GenerateHashRequest request)
        {
            try
            {
                // Generar hash con SHA256
                string hash = _authService.HashPassword(request.Password);

                // Verificar que el hash funciona
                bool verifyResult = _authService.VerifyPassword(request.Password, hash);

                return Ok(new
                {
                    password = request.Password,
                    hashGenerado = hash,
                    hashLength = hash.Length,
                    verificacion = verifyResult ? "✅ Hash generado correctamente" : "❌ Error en generación",
                    sqlScript = $@"
-- Ejecuta esto en SQL Server:
USE PuntosDeVentaDB;
GO

DELETE FROM Usuarios WHERE Username = 'admin';
GO

INSERT INTO Usuarios (Username, PasswordHash, Rol, Activo)
VALUES ('admin', '{hash}', 'Admin', 1);
GO

SELECT * FROM Usuarios WHERE Username = 'admin';
GO
"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// DTO para solicitud de validación de token
    /// </summary>
    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para prueba de hash (TEMPORAL - solo desarrollo)
    /// </summary>
    public class TestHashRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para generar hash (TEMPORAL - solo desarrollo)
    /// </summary>
    public class GenerateHashRequest
    {
        public string Password { get; set; } = string.Empty;
    }

}