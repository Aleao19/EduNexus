using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace EduNexus.UI.Models
{
    // RAV-12-001: guarda el material de cada clase fuera de las carpetas públicas
    // (App_Data/MaterialesClase/{seccion}-{materia}/{nombre_interno}).
    public static class AlmacenamientoMaterial
    {
        public const int TamanoMaximo = 10 * 1024 * 1024;
        public const string TamanoMaximoTexto = "10 MB";
        public const string FormatosTexto = "PDF, Word (.doc, .docx), PowerPoint (.ppt, .pptx), Excel (.xls, .xlsx), JPG y PNG";

        private static readonly string[] Extensiones =
            { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };

        private static readonly Regex NombreInternoValido =
            new Regex(@"^[0-9a-f]{32}\.[a-z0-9]{3,4}$", RegexOptions.CultureInvariant);

        public static string CarpetaBase
        {
            get
            {
                return Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "App_Data", "MaterialesClase"));
            }
        }

        // Escenario 4: devuelve el mensaje de error, o null si el archivo es válido
        public static string Validar(HttpPostedFileBase archivo)
        {
            string detalle = "Formatos permitidos: " + FormatosTexto + ". Tamaño máximo: " + TamanoMaximoTexto + ".";

            if (archivo.ContentLength > TamanoMaximo)
            {
                return "El archivo supera el tamaño máximo permitido. " + detalle;
            }

            string extension = AlmacenamientoDocumentoExpediente.ExtensionSegura(archivo.FileName);
            if (!Extensiones.Contains(extension))
            {
                return "Formato de archivo no permitido. " + detalle;
            }

            // Se valida la firma real del archivo, no solo la extensión
            Stream contenido = archivo.InputStream;
            if (!contenido.CanSeek)
            {
                return "No se puede validar el archivo recibido.";
            }

            byte[] enc = new byte[8];
            contenido.Position = 0;
            int leidos = contenido.Read(enc, 0, enc.Length);
            contenido.Position = 0;

            bool firmaValida;
            switch (extension)
            {
                case ".pdf":
                    firmaValida = leidos >= 5 && enc[0] == 0x25 && enc[1] == 0x50 && enc[2] == 0x44 && enc[3] == 0x46 && enc[4] == 0x2D;
                    break;
                case ".jpg":
                case ".jpeg":
                    firmaValida = leidos >= 3 && enc[0] == 0xFF && enc[1] == 0xD8 && enc[2] == 0xFF;
                    break;
                case ".png":
                    firmaValida = leidos >= 8 && enc.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
                    break;
                case ".docx":
                case ".pptx":
                case ".xlsx":
                    firmaValida = leidos >= 4 && enc[0] == 0x50 && enc[1] == 0x4B && enc[2] == 0x03 && enc[3] == 0x04;
                    break;
                default: // .doc, .ppt, .xls
                    firmaValida = leidos >= 8 && enc[0] == 0xD0 && enc[1] == 0xCF && enc[2] == 0x11 && enc[3] == 0xE0
                               && enc[4] == 0xA1 && enc[5] == 0xB1 && enc[6] == 0x1A && enc[7] == 0xE1;
                    break;
            }

            return firmaValida ? null : "El contenido del archivo no coincide con su formato. " + detalle;
        }

        // Guarda el archivo y devuelve el nombre interno con el que quedó en disco
        public static string Guardar(HttpPostedFileBase archivo, int idSeccion, int idMateria)
        {
            string extension = AlmacenamientoDocumentoExpediente.ExtensionSegura(archivo.FileName);
            string nombreInterno = Guid.NewGuid().ToString("N") + extension;

            string carpeta = Path.Combine(CarpetaBase, idSeccion + "-" + idMateria);
            Directory.CreateDirectory(carpeta);

            archivo.SaveAs(Path.Combine(carpeta, nombreInterno));
            return nombreInterno;
        }

        // Ruta completa del archivo, o null si el nombre no es válido
        public static string RutaArchivo(int idSeccion, int idMateria, string nombreInterno)
        {
            if (string.IsNullOrEmpty(nombreInterno) || !NombreInternoValido.IsMatch(nombreInterno))
            {
                return null;
            }

            string ruta = Path.GetFullPath(Path.Combine(CarpetaBase, idSeccion + "-" + idMateria, nombreInterno));
            return ruta.StartsWith(CarpetaBase, StringComparison.OrdinalIgnoreCase) ? ruta : null;
        }

        public static void Eliminar(int idSeccion, int idMateria, string nombreInterno)
        {
            string ruta = RutaArchivo(idSeccion, idMateria, nombreInterno);
            if (ruta != null && File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }

        public static string TipoMime(string extension)
        {
            switch (extension)
            {
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".ppt": return "application/vnd.ms-powerpoint";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                default: return "application/octet-stream";
            }
        }
    }
}