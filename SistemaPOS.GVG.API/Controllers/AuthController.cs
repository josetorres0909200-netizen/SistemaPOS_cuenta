using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
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
    }

    /// <summary>
    /// DTO para solicitud de validación de token
    /// </summary>
    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para registro de usuarios
    /// </summary>
    public class RegisterDTO
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? Rol { get; set; }
    }
}