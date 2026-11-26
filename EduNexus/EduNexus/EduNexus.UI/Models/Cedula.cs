using System.Linq;
using System.Text.RegularExpressions;

namespace EduNexus.UI.Models
{
    // Formato estándar de la cédula de identidad de Costa Rica: 0-0000-0000
    // (9 dígitos: provincia, tomo y asiento). Se acepta escrita con o sin
    // guiones o espacios, pero siempre se guarda con el formato estándar.
    public static class Cedula
    {
        public const string Formato = "0-0000-0000";
        public const string Patron = @"^\d-\d{4}-\d{4}$";
        public const string MensajeFormato = "La cédula debe tener el formato 0-0000-0000 (9 dígitos).";

        // Devuelve la cédula con formato 0-0000-0000, o null si no tiene 9 dígitos.
        public static string Normalizar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            string digitos = new string(valor.Where(char.IsDigit).ToArray());
            if (digitos.Length != 9) return null;
            // Si el usuario escribió letras u otros símbolos, no es una cédula válida.
            if (Regex.IsMatch(valor.Trim(), @"[^\d\s\-]")) return null;
            return digitos.Substring(0, 1) + "-" + digitos.Substring(1, 4) + "-" + digitos.Substring(5, 4);
        }

        public static bool EsValida(string valor)
        {
            return Normalizar(valor) != null;
        }

        // Para comparar cédulas guardadas en formatos viejos (con o sin guiones).
        public static bool Iguales(string a, string b)
        {
            string na = Normalizar(a) ?? (a ?? "").Trim();
            string nb = Normalizar(b) ?? (b ?? "").Trim();
            return na == nb;
        }
    }
}
