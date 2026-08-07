using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemaPOS.Desktop.Services
{
    /// <summary>
    /// Helper para cifrar/descifrar tokens usando Windows DPAPI (Data Protection API)
    /// </summary>
    public static class TokenSecurityHelper
    {
        // Entropía adicional para mayor seguridad (salt único de la aplicación)
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("SistemaPOS_GVG_2024_SecureToken");

        /// <summary>
        /// Cifra un token usando DPAPI (solo puede descifrarse en la misma máquina y usuario de Windows)
        /// </summary>
        /// <param name="plainToken">Token JWT en texto plano</param>
        /// <returns>Token cifrado en Base64</returns>
        public static string ProtectToken(string plainToken)
        {
            if (string.IsNullOrEmpty(plainToken))
                return string.Empty;

            try
            {
                byte[] tokenBytes = Encoding.UTF8.GetBytes(plainToken);
                byte[] encryptedBytes = ProtectedData.Protect(
                    tokenBytes,
                    _entropy,
                    DataProtectionScope.CurrentUser
                );

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Error al cifrar el token", ex);
            }
        }

        /// <summary>
        /// Descifra un token previamente cifrado con ProtectToken
        /// </summary>
        /// <param name="encryptedToken">Token cifrado en Base64</param>
        /// <returns>Token JWT en texto plano</returns>
        public static string UnprotectToken(string encryptedToken)
        {
            if (string.IsNullOrEmpty(encryptedToken))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedToken);
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    _entropy,
                    DataProtectionScope.CurrentUser
                );

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Error al descifrar el token. El token puede estar corrupto o haber sido generado en otra máquina/usuario.", ex);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Token con formato inválido", ex);
            }
        }

        /// <summary>
        /// Verifica si un token cifrado es válido (puede descifrarse)
        /// </summary>
        public static bool IsValidEncryptedToken(string encryptedToken)
        {
            if (string.IsNullOrEmpty(encryptedToken))
                return false;

            try
            {
                UnprotectToken(encryptedToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
