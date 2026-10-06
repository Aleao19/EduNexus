using System;
using System.Security.Cryptography;

namespace EduNexus.UI.Models
{
    // Cifrado de contraseñas con PBKDF2 (SHA-256). Nunca se guarda la contraseña en texto plano.
    // Formato que genera el sistema:  PBKDF2$iteraciones$salBase64$hashBase64
    // Para compatibilidad, Verificar también acepta hashes de ASP.NET Identity (v2 y v3).
    public static class ContrasennaHasher
    {
        private const int Iteraciones = 10000;
        private const int TamannoSal = 16;
        private const int TamannoHash = 32;

        public static string Cifrar(string contrasenna)
        {
            byte[] sal = new byte[TamannoSal];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(sal);
            }

            byte[] hash = Derivar(contrasenna, sal, Iteraciones, HashAlgorithmName.SHA256, TamannoHash);
            return string.Format("PBKDF2${0}${1}${2}", Iteraciones, Convert.ToBase64String(sal), Convert.ToBase64String(hash));
        }

        public static bool Verificar(string contrasenna, string guardado)
        {
            if (string.IsNullOrEmpty(contrasenna) || string.IsNullOrEmpty(guardado))
            {
                return false;
            }

            try
            {
                if (guardado.StartsWith("PBKDF2$", StringComparison.Ordinal))
                {
                    string[] partes = guardado.Split('$');
                    if (partes.Length != 4)
                    {
                        return false;
                    }
                    int iteraciones = int.Parse(partes[1]);
                    byte[] sal = Convert.FromBase64String(partes[2]);
                    byte[] esperado = Convert.FromBase64String(partes[3]);
                    return Iguales(esperado, Derivar(contrasenna, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length));
                }

                byte[] datos = Convert.FromBase64String(guardado);

                // ASP.NET Identity v2: 0x00 | sal(16) | subclave(32), PBKDF2-SHA1 con 1000 iteraciones
                if (datos.Length == 49 && datos[0] == 0x00)
                {
                    byte[] sal = new byte[16];
                    byte[] esperado = new byte[32];
                    Buffer.BlockCopy(datos, 1, sal, 0, 16);
                    Buffer.BlockCopy(datos, 17, esperado, 0, 32);
                    return Iguales(esperado, Derivar(contrasenna, sal, 1000, HashAlgorithmName.SHA1, 32));
                }

                // ASP.NET Identity v3: 0x01 | prf(4) | iteraciones(4) | tamañoSal(4) | sal | subclave
                if (datos.Length > 13 && datos[0] == 0x01)
                {
                    int prf = LeerEntero(datos, 1);
                    int iteraciones = LeerEntero(datos, 5);
                    int tamannoSal = LeerEntero(datos, 9);
                    if (tamannoSal < 8 || 13 + tamannoSal >= datos.Length)
                    {
                        return false;
                    }
                    byte[] sal = new byte[tamannoSal];
                    Buffer.BlockCopy(datos, 13, sal, 0, tamannoSal);
                    byte[] esperado = new byte[datos.Length - 13 - tamannoSal];
                    Buffer.BlockCopy(datos, 13 + tamannoSal, esperado, 0, esperado.Length);
                    HashAlgorithmName algoritmo = prf == 0 ? HashAlgorithmName.SHA1
                                                : prf == 1 ? HashAlgorithmName.SHA256
                                                : HashAlgorithmName.SHA512;
                    return Iguales(esperado, Derivar(contrasenna, sal, iteraciones, algoritmo, esperado.Length));
                }
            }
            catch (FormatException)
            {
                // El valor guardado no es un hash válido
            }

            return false;
        }

        private static int LeerEntero(byte[] datos, int inicio)
        {
            return (datos[inicio] << 24) | (datos[inicio + 1] << 16) | (datos[inicio + 2] << 8) | datos[inicio + 3];
        }

        private static bool Iguales(byte[] a, byte[] b)
        {
            // Comparación en tiempo constante
            int diferencia = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diferencia |= a[i] ^ b[i];
            }
            return diferencia == 0;
        }

        private static byte[] Derivar(string contrasenna, byte[] sal, int iteraciones, HashAlgorithmName algoritmo, int tamanno)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(contrasenna, sal, iteraciones, algoritmo))
            {
                return pbkdf2.GetBytes(tamanno);
            }
        }
    }
}
