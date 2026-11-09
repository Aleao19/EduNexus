
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace EduNexus.UI.Models
{
    // REXP-03-002: guarda los documentos del expediente fuera de las carpetas públicas
    // (App_Data/DocumentosExpedientes/{id_expediente}/{nombre_interno}).
    public static class AlmacenamientoDocumentoExpediente
    {
        // El Web.config usa el maxRequestLength por defecto (4096 KB): el límite real es 4 MB.
        public const int TamanoMaximo = 4 * 1024 * 1024;
        public const string TamanoMaximoTexto = "4 MB";

        public static readonly string[] ExtensionesPermitidas =
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

        private static readonly Regex NombreInternoValido =
            new Regex(@"^[0-9a-f]{32}\.(pdf|jpg|jpeg|png)$", RegexOptions.CultureInvariant);

        public static string CarpetaBase
        {
            get
            {
                return Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "App_Data",
                    "DocumentosExpedientes"
                ));
            }
        }

        // Nombre del archivo sin la ruta del cliente. No usa Path.GetFileName
        // porque lanza excepción con caracteres inválidos en el nombre.
        public static string NombreSeguro(string nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                return "";
            }

            string nombre = nombreArchivo.Trim();
            int corte = Math.Max(nombre.LastIndexOf('\\'), nombre.LastIndexOf('/'));

            if (corte >= 0)
            {
                nombre = nombre.Substring(corte + 1);
            }

            char[] invalidos = Path.GetInvalidFileNameChars();
            nombre = new string(nombre.Where(c => !invalidos.Contains(c) && !char.IsControl(c)).ToArray()).Trim();

            return nombre.Length <= 255 ? nombre : nombre.Substring(nombre.Length - 255);
        }

        // Extensión en minúsculas (con punto) sin lanzar excepciones.
        public static string ExtensionSegura(string nombreArchivo)
        {
            string nombre = NombreSeguro(nombreArchivo);
            int punto = nombre.LastIndexOf('.');

            if (punto < 0 || punto == nombre.Length - 1)
            {
                return "";
            }

            return nombre.Substring(punto).ToLowerInvariant();
        }

        public static bool ExtensionPermitida(string extension)
        {
            return ExtensionesPermitidas.Contains(extension);
        }

        // Tipo MIME según la extensión validada (no se confía en el que envía el navegador).
        public static string TipoMime(string extension)
        {
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                default:
                    return "application/octet-stream";
            }
        }

        public static string GuardarDocumento(
            HttpPostedFileBase archivo,
            int id_expediente)
        {
            if (id_expediente <= 0)
            {
                throw new ArgumentException(
                    "El expediente no es válido."
                );
            }

            if (archivo == null ||
                archivo.ContentLength <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar un documento."
                );
            }

            if (archivo.ContentLength > TamanoMaximo)
            {
                throw new ArgumentException(
                    "El archivo supera el tamaño permitido de " + TamanoMaximoTexto + "."
                );
            }

            string extension = ExtensionSegura(archivo.FileName);

            if (!ExtensionPermitida(extension))
            {
                throw new ArgumentException(
                    "Formato de documento no permitido."
                );
            }

            // Validar la firma real del archivo.
            byte[] encabezado = new byte[8];

            Stream contenido = archivo.InputStream;

            if (!contenido.CanSeek)
            {
                throw new ArgumentException(
                    "No se puede validar el archivo recibido."
                );
            }

            contenido.Position = 0;

            int bytesLeidos = contenido.Read(
                encabezado,
                0,
                encabezado.Length
            );

            contenido.Position = 0;

            bool formatoValido = false;

            if (extension == ".pdf")
            {
                formatoValido =
                    bytesLeidos >= 5 &&
                    encabezado[0] == 0x25 &&
                    encabezado[1] == 0x50 &&
                    encabezado[2] == 0x44 &&
                    encabezado[3] == 0x46 &&
                    encabezado[4] == 0x2D;
            }
            else if (extension == ".jpg" ||
                     extension == ".jpeg")
            {
                formatoValido =
                    bytesLeidos >= 3 &&
                    encabezado[0] == 0xFF &&
                    encabezado[1] == 0xD8 &&
                    encabezado[2] == 0xFF;
            }
            else if (extension == ".png")
            {
                byte[] firmaPng =
                {
                    137, 80, 78, 71,
                    13, 10, 26, 10
                };

                formatoValido =
                    bytesLeidos >= 8 &&
                    firmaPng.SequenceEqual(encabezado);
            }

            if (!formatoValido)
            {
                throw new ArgumentException(
                    "El contenido del archivo no coincide con su formato."
                );
            }

            string carpetaExpediente = Path.Combine(
                CarpetaBase,
                id_expediente.ToString()
            );

            Directory.CreateDirectory(carpetaExpediente);

            // Nombre interno único para evitar sobrescrituras.
            string nombreInterno =
                Guid.NewGuid().ToString("N") + extension;

            archivo.SaveAs(Path.Combine(
                carpetaExpediente,
                nombreInterno
            ));

            // Retornar solo el nombre interno,
            // nunca la ruta física del servidor.
            return nombreInterno;
        }

        // Ruta física de un documento guardado. Devuelve null si el nombre interno
        // no tiene el formato esperado o si la ruta queda fuera de la carpeta base.
        public static string RutaDocumento(int id_expediente, string nombreInterno)
        {
            if (id_expediente <= 0 ||
                string.IsNullOrEmpty(nombreInterno) ||
                !NombreInternoValido.IsMatch(nombreInterno))
            {
                return null;
            }

            string baseCompleta = CarpetaBase.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            string ruta = Path.GetFullPath(Path.Combine(
                CarpetaBase,
                id_expediente.ToString(),
                nombreInterno
            ));

            return ruta.StartsWith(baseCompleta, StringComparison.OrdinalIgnoreCase)
                ? ruta
                : null;
        }

        // Se usa cuando el registro en la base de datos falla después de guardar el archivo.
        public static void EliminarDocumento(int id_expediente, string nombreInterno)
        {
            string ruta = RutaDocumento(id_expediente, nombreInterno);

            if (ruta != null && File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
    }
}
