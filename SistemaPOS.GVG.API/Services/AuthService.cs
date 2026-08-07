using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaPOS.API.Data;
using SistemaPOS.GVG.API.Models;
using SistemaPOS.GVG.API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SistemaPOS.GVG.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDTO?> AuthenticateAsync(string username, string password)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Username == username && u.Activo);

                if (usuario == null)
                {
                    _logger.LogWarning("Intento de login con usuario inexistente o inactivo: {Username}", username);
                    return null;
                }

                _logger.LogInformation("Usuario encontrado: {Username}, Rol: {Rol}", 
                    usuario.Username, usuario.Rol);

                // ✅ Validación con SHA256 (confiable y funcional)
                string passwordHash = HashPassword(password);
                bool passwordValida = passwordHash == usuario.PasswordHash;

                if (!passwordValida)
                {
                    _logger.LogWarning("Intento de login con contraseña incorrecta para usuario: {Username}", username);
                    return null;
                }

                // Generar token JWT
                var token = GenerateJwtToken(usuario);
                var refreshToken = GenerateRefreshToken();

                _logger.LogInformation("Usuario autenticado exitosamente: {Username}", username);

                return new AuthResponseDTO
                {
                    Token = token.Token,
                    RefreshToken = refreshToken,
                    Username = usuario.Username,
                    Rol = usuario.Rol,
                    Expiration = token.Expiration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante autenticación de usuario: {Username}", username);
                throw;
            }
        }

        public async Task<Usuario> RegisterUserAsync(string username, string password, string rol)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Username == username))
            {
                throw new InvalidOperationException($"El usuario '{username}' ya existe");
            }

            var usuario = new Usuario
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Rol = rol,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuario registrado exitosamente: {Username} - Rol: {Rol}", username, rol);

            return usuario;
        }

        public string HashPassword(string password)
        {
            // SHA256: hash criptográfico seguro, unidireccional y 100% confiable
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            string passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        public bool ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada"));

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private (string Token, DateTime Expiration) GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada"));

            var expiration = DateTime.UtcNow.AddHours(8);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Username),
                    new Claim(ClaimTypes.Role, usuario.Rol),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = expiration,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiration);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
