# 🎯 GUÍA RÁPIDA DE IMPLEMENTACIÓN
## SistemaPOS - Post Auditoría de QA

---

## 📋 PASOS PARA PONER EN PRODUCCIÓN

### 1️⃣ **Aplicar Migración de Base de Datos**
```bash
cd SistemaPOS.GVG.API
dotnet ef database update
```

✅ Esto creará:
- 15 índices optimizados
- Usuario `admin` con contraseña hasheada
- Configuración de precisión decimal

---

### 2️⃣ **Migrar Contraseñas Existentes (Si tienes usuarios en BD)**

#### Opción A: Usando el endpoint (Recomendado)
1. Ejecutar la API
2. Hacer login como admin:
   ```json
   POST /api/auth/login
   {
	 "username": "admin",
	 "password": "Admin123!"
   }
   ```
3. Copiar el token JWT
4. Ejecutar migración:
   ```json
   POST /api/migration/migrate-passwords
   Headers: { "Authorization": "Bearer TU_TOKEN_AQUI" }
   ```

#### Opción B: Script SQL manual
```sql
-- ⚠️ BACKUP PRIMERO
SELECT * INTO Usuarios_BACKUP FROM Usuarios

-- Las contraseñas existentes ya no funcionarán después de la migración
-- Los usuarios deberán resetear sus contraseñas
```

---

### 3️⃣ **Configurar Variables de Entorno (Producción)**

#### `appsettings.Production.json`
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=TU_SERVIDOR;Database=PuntosDeVentaDB;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;"
  },
  "Jwt": {
	"Key": "CAMBIAR_ESTA_CLAVE_POR_UNA_SEGURA_MINIMO_32_CARACTERES_ALEATORIOS",
	"Issuer": "SistemaPOS.API",
	"Audience": "SistemaPOS.Clients",
	"ExpirationHours": 4
  },
  "Serilog": {
	"MinimumLevel": {
	  "Default": "Warning",
	  "Override": {
		"Microsoft": "Error",
		"System": "Error"
	  }
	}
  }
}
```

⚠️ **IMPORTANTE:** Generar clave JWT segura:
```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

---

### 4️⃣ **Actualizar Cliente WPF**

#### Modificar URL en producción:
**Ubicación:** `SistemaPOS.GVG\Services\ApiClient.cs`

```csharp
private readonly string _baseUrl = "https://TU_SERVIDOR_PRODUCCION/api/";
```

**O mejor:** Usar configuración:
```csharp
private readonly string _baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "https://localhost:7269/api/";
```

---

### 5️⃣ **Eliminar Código Temporal (Después de migración)**

#### Archivos a eliminar:
1. `Controllers/MigrationController.cs`
2. `Utilities/PasswordMigrationUtility.cs`

#### Código a remover de `Program.cs`:
```csharp
// ELIMINAR ESTA LÍNEA:
builder.Services.AddScoped<PasswordMigrationUtility>();
```

---

## 🔐 CREDENCIALES POR DEFECTO

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin | Admin123! | Admin |

⚠️ **ACCIÓN REQUERIDA:**
1. Iniciar sesión con estas credenciales
2. Crear usuario personal
3. **Desactivar o cambiar contraseña del usuario `admin`**

```json
POST /api/auth/register
Headers: { "Authorization": "Bearer TOKEN_ADMIN" }
{
  "username": "tu_usuario",
  "password": "TuContraseñaSegura123!",
  "rol": "Admin"
}
```

---

## ✅ CHECKLIST DE VERIFICACIÓN POST-DEPLOY

### Base de Datos
- [ ] Migración aplicada exitosamente
- [ ] Índices creados (verificar en SSMS)
- [ ] Usuario admin existe
- [ ] Contraseñas migradas (si aplica)
- [ ] Backup realizado antes de cambios

### API
- [ ] Compilación exitosa sin warnings
- [ ] Logs se están generando en `logs/`
- [ ] Health check responde: `GET /health`
- [ ] Swagger accesible: `/swagger` (solo development)
- [ ] JWT se genera correctamente en login
- [ ] Rate limiting funciona (intentar 6 logins fallidos)

### Seguridad
- [ ] Clave JWT cambiada en producción
- [ ] CORS configurado con origins correctos
- [ ] HTTPS habilitado
- [ ] Contraseña admin cambiada
- [ ] MigrationController eliminado

### Cliente WPF
- [ ] URL de API actualizada
- [ ] Login funciona correctamente
- [ ] Token se almacena en sesión
- [ ] Endpoints protegidos requieren autenticación
- [ ] Manejo de sesión expirada funciona

---

## 🧪 PRUEBAS FUNCIONALES

### 1. Test de Autenticación
```bash
# Login exitoso
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin123!"
}

# Esperado: 200 OK + Token JWT
```

### 2. Test de Autorización
```bash
# Sin token (debe fallar)
GET /api/productos

# Esperado: 401 Unauthorized

# Con token (debe funcionar)
GET /api/productos
Headers: { "Authorization": "Bearer TOKEN_AQUI" }

# Esperado: 200 OK + Lista de productos
```

### 3. Test de Rate Limiting
```bash
# Hacer 6 requests de login en < 1 minuto con contraseña incorrecta
# Esperado: Request #6 -> 429 Too Many Requests
```

### 4. Test de Transacciones
```bash
# Crear venta con producto sin stock
POST /api/ventas
{
  "detalles": [
	{ "idProducto": 1, "cantidad": 99999 }
  ]
}

# Esperado: 400 Bad Request + "Stock insuficiente"
# Verificar: Stock no cambió en BD (transacción revertida)
```

### 5. Test de Health Check
```bash
GET /health

# Esperado: 200 OK
{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
	"database": {
	  "status": "Healthy"
	}
  }
}
```

---

## 📊 MONITOREO EN PRODUCCIÓN

### Logs
**Ubicación:** `SistemaPOS.GVG.API/logs/sistemaPOS-YYYY-MM-DD.txt`

**Qué buscar:**
- `[ERR]` - Errores críticos
- `[WRN]` - Advertencias (requests lentos, intentos fallidos)
- `[INF]` - Información (operaciones exitosas)

**Ejemplo:**
```
[2026-01-15 10:23:45 INF] Usuario autenticado exitosamente: admin
[2026-01-15 10:24:12 WRN] Intento de login fallido para usuario: hacker
[2026-01-15 10:25:33 ERR] Error al crear venta. Transacción revertida
```

### Métricas de Rendimiento
Los logs incluyen tiempos de respuesta:
```
[INF] GET /api/productos - 45ms - Status: 200
[WRN] POST /api/ventas - 1203ms - Status: 200  ⚠️ Request lento
```

---

## 🚨 TROUBLESHOOTING

### Problema: "Usuario o contraseña incorrectos" después de migración

**Causa:** Contraseñas no migradas

**Solución:**
1. Verificar en BD: `SELECT Username, LEFT(PasswordHash, 4) FROM Usuarios`
2. Si no empieza con `$2a$`, ejecutar migración
3. Alternativamente, crear usuarios nuevos

---

### Problema: "401 Unauthorized" en todos los endpoints

**Causa:** JWT mal configurado o token no enviado

**Solución:**
```csharp
// En ApiClient (WPF):
_apiClient.SetAuthToken(tokenFromLogin);

// Verificar headers:
_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

---

### Problema: "429 Too Many Requests"

**Causa:** Rate limiting activo

**Solución:**
- Esperar 1 minuto
- O aumentar límite en `appsettings.json`:
```json
"IpRateLimiting": {
  "GeneralRules": [
	{ "Endpoint": "*/api/auth/login", "Period": "1m", "Limit": 10 }
  ]
}
```

---

### Problema: Venta se registra pero stock no se descuenta

**Causa:** Transacción no commit o rollback silencioso

**Solución:**
1. Revisar logs: buscar `"Transacción revertida"`
2. Verificar que `VentaService.CreateAsync` usa transacciones
3. Verificar que no hay excepciones silenciosas

---

## 📚 ENDPOINTS DOCUMENTADOS

### Autenticación (Sin autenticación requerida)

#### `POST /api/auth/login`
**Request:**
```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

**Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5...",
  "refreshToken": "base64RefreshToken",
  "username": "admin",
  "rol": "Admin",
  "expiration": "2026-01-15T18:00:00Z"
}
```

---

#### `POST /api/auth/register` (Requiere Admin)
**Request:**
```json
{
  "username": "nuevo_usuario",
  "password": "Password123!",
  "rol": "Vendedor"
}
```

**Roles válidos:** `Admin`, `Gerente`, `Vendedor`

---

### Productos (Requiere autenticación)

#### `GET /api/productos`
**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5...
```

**Response (200):**
```json
{
  "success": true,
  "message": "Se encontraron 10 productos",
  "data": [
	{
	  "idProducto": 1,
	  "codigoBarras": "123456",
	  "descripcion": "Pintura Roja",
	  "categoria": "Pinturas",
	  "precioVenta": 150.00,
	  "stock": 25
	}
  ]
}
```

---

#### `POST /api/productos` (Admin/Gerente)
**Request:**
```json
{
  "codigoBarras": "789012",
  "descripcion": "Pintura Azul",
  "categoria": "Pinturas",
  "acabado": "Mate",
  "tamanio": "1L",
  "precioCosto": 80.00,
  "precioVenta": 150.00,
  "stock": 10
}
```

---

### Ventas (Requiere autenticación)

#### `POST /api/ventas`
**Request:**
```json
{
  "idCliente": null,
  "detalles": [
	{
	  "idProducto": 1,
	  "cantidad": 2,
	  "precioUnitario": 150.00
	}
  ],
  "impuesto": 48.00,
  "pagado": 400.00,
  "tipoPago": "Efectivo",
  "observaciones": "Cliente frecuente"
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Venta registrada exitosamente",
  "data": {
	"idVenta": 123,
	"total": 348.00,
	"cambio": 52.00,
	"fechaVenta": "2026-01-15T10:30:00"
  }
}
```

---

## 🎓 MEJORES PRÁCTICAS IMPLEMENTADAS

### ✅ Seguridad
- BCrypt para contraseñas (workFactor 12)
- JWT con expiración configurable
- Rate limiting en endpoints sensibles
- CORS restrictivo
- Logs de intentos fallidos

### ✅ Arquitectura
- Separación de responsabilidades (Services, Controllers, DTOs)
- Inyección de dependencias
- Transacciones atómicas
- Middleware personalizado

### ✅ Calidad de Código
- FluentValidation para reglas de negocio
- Manejo global de excepciones
- Logging estructurado
- DTOs separados de entidades

### ✅ Rendimiento
- 15 índices estratégicos en BD
- Queries optimizadas
- Monitoreo de tiempos de respuesta
- Health checks para detección temprana

---

## 📞 CONTACTO Y SOPORTE

Para consultas técnicas:
1. Revisar logs en `logs/`
2. Verificar health check: `/health`
3. Consultar este documento
4. Revisar `AUDITORIA_QA_COMPLETA.md`

---

**✅ SISTEMA LISTO PARA PRODUCCIÓN**

*Última actualización: 2026*
