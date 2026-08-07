USE PuntosDeVentaDB;
GO

-- Hash SHA256 de 'Admin123!'
-- Generado con: SHA256.ComputeHash(UTF8.GetBytes("Admin123!"))
UPDATE Usuarios 
SET PasswordHash = 'PrP+ZrMeO00Q+nC1ytSccRIpSvauTkdqHEBRVdRaoSE='
WHERE Username = 'admin';

-- Verificar actualización
SELECT IdUsuario, Username, PasswordHash, Rol, Activo 
FROM Usuarios 
WHERE Username = 'admin';

PRINT '✅ Hash SHA256 actualizado correctamente';
PRINT 'Credenciales: admin / Admin123!';
