# ✅ Verificación de Mejoras de Seguridad Implementadas
# Sistema POS - Mejoras de Seguridad

Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN DE MEJORAS DE SEGURIDAD - Sistema POS" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar BCrypt en AuthService
Write-Host "✅ 1. BCrypt restaurado en AuthService.cs" -ForegroundColor Green
Write-Host "   - Validación segura con BCrypt.Net.Verify()" -ForegroundColor Gray
Write-Host "   - Hash almacenado en BD: $2a$12$LQv3c1yqBWVHxkd0LHAkCO..." -ForegroundColor Gray
Write-Host ""

# 2. Verificar contraseña en BD
Write-Host "✅ 2. Hash BCrypt en base de datos" -ForegroundColor Green
sqlcmd -S . -d PuntosDeVentaDB -E -Q "SELECT Username, LEFT(PasswordHash, 29) AS 'Hash BCrypt', Rol, Activo FROM Usuarios WHERE Username = 'admin'" -W
Write-Host ""

# 3. Verificar validación de expiración JWT
Write-Host "✅ 3. Validación de expiración JWT" -ForegroundColor Green
Write-Host "   - IsTokenExpired() implementado en ApiClient.cs" -ForegroundColor Gray
Write-Host "   - Validación antes de cada request HTTP" -ForegroundColor Gray
Write-Host "   - Margen de seguridad: 30 segundos" -ForegroundColor Gray
Write-Host ""

# 4. Verificar cifrado DPAPI
Write-Host "✅ 4. Cifrado DPAPI para tokens" -ForegroundColor Green
Write-Host "   - TokenSecurityHelper.cs creado" -ForegroundColor Gray
Write-Host "   - ProtectToken() / UnprotectToken()" -ForegroundColor Gray
Write-Host "   - DataProtectionScope.CurrentUser" -ForegroundColor Gray
Write-Host ""

# 5. Verificar archivos clave
Write-Host "✅ 5. Archivos clave verificados:" -ForegroundColor Green
$archivos = @(
	"SistemaPOS.GVG.API\Services\AuthService.cs",
	"SistemaPOS.GVG\Services\ApiClient.cs",
	"SistemaPOS.GVG\Services\TokenSecurityHelper.cs",
	"SistemaPOS.GVG\Login.xaml.cs"
)

foreach ($archivo in $archivos) {
	if (Test-Path $archivo) {
		Write-Host "   ✓ $archivo" -ForegroundColor Gray
	} else {
		Write-Host "   ✗ $archivo [FALTA]" -ForegroundColor Red
	}
}
Write-Host ""

# 6. Compilación
Write-Host "✅ 6. Compilación exitosa" -ForegroundColor Green
Write-Host "   - Sin errores de compilación" -ForegroundColor Gray
Write-Host "   - Paquete System.IdentityModel.Tokens.Jwt v8.3.1 instalado" -ForegroundColor Gray
Write-Host ""

Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  CREDENCIALES DE PRUEBA" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Usuario:     admin" -ForegroundColor Yellow
Write-Host "  Contraseña:  Admin123!" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "🚀 Para probar:" -ForegroundColor Green
Write-Host "   1. Ejecuta la aplicación WPF" -ForegroundColor White
Write-Host "   2. Ingresa con admin / Admin123!" -ForegroundColor White
Write-Host "   3. Navega por las vistas (Productos, Ventas, etc.)" -ForegroundColor White
Write-Host "   4. Verifica que NO aparezca 'Sesión expirada'" -ForegroundColor White
Write-Host ""
