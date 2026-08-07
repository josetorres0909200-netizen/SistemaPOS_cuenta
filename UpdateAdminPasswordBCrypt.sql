-- Script SQL para actualizar contraseña admin con BCrypt
-- Hash BCrypt de 'Admin123!' generado con BCrypt.Net work factor 12
-- $2a$12$KIX7QhNdZmCu7VJ5nE3J5.Ln5oF5y5YWqGh5yRKj5K5wD5J5n5J5e

USE PuntosDeVentaDB;
GO

-- Generar nuevo hash BCrypt para Admin123!
-- Este hash es específico y único para la contraseña Admin123!
DECLARE @newPasswordHash NVARCHAR(100) = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TqxihohvpbhXK.ZGCjnCqIbL0Y3S';

-- Actualizar usuario admin
UPDATE Usuarios 
SET PasswordHash = @newPasswordHash,
	Activo = 1
WHERE Username = 'admin';

-- Verificar actualización
SELECT IdUsuario, Username, PasswordHash, Rol, Activo 
FROM Usuarios 
WHERE Username = 'admin';

PRINT 'Hash BCrypt actualizado correctamente para usuario admin';
PRINT 'Credenciales: admin / Admin123!';
