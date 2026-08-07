using System;

// Programa temporal para generar hash BCrypt de Admin123!
// Ejecutar: dotnet script GenerarHashBCrypt.csx

string password = "Admin123!";
string hash = BCrypt.Net.BCrypt.HashPassword(password, 12);

Console.WriteLine("==============================================");
Console.WriteLine("Hash BCrypt generado:");
Console.WriteLine(hash);
Console.WriteLine("==============================================");
Console.WriteLine($"Para contraseña: {password}");
Console.WriteLine("Work factor: 12");
