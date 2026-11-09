using System.Security.Cryptography;
using System.Text;

namespace PlayaAutos.API.Helpers
{
    /// <summary>
    /// Hash de contraseñas con el mismo formato que AuthController y
    /// UsuariosController (SHA-256 en hexadecimal, minúsculas), para poder
    /// verificar la contraseña actual en operaciones sensibles como el cambio
    /// de email del cliente.
    /// </summary>
    public static class Contrasenas
    {
        public static string Hash(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        public static bool Coincide(string? password, string hashGuardado) =>
            !string.IsNullOrEmpty(password) &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Hash(password)),
                Encoding.UTF8.GetBytes(hashGuardado ?? ""));
    }
}
