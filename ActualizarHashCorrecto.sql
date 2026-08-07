USE PuntosDeVentaDB;
GO

-- Hash BCrypt correcto para 'Admin123!' generado y verificado
UPDATE Usuarios 
SET PasswordHash = '$2a$12$mVkGCk6RTXOvEgQ1QHGE7Odxi7xXeI1Erd24XX1B0LoJTMos6d0rS'
WHERE Username = 'admin';

-- Verificar actualización
SELECT IdUsuario, Username, PasswordHash, Rol, Activo 
FROM Usuarios 
WHERE Username = 'admin';

PRINT 'Hash BCrypt correcto actualizado para Admin123!';
