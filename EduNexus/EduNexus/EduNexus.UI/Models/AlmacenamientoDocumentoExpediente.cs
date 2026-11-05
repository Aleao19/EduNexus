
using System;
using System.IO;
using System.Linq;
using System.Web;

namespace EduNexus.UI.Models
{
    public static class AlmacenamientoDocumentoExpediente
    {
        private const int TamanoMaximo = 5 * 1024 * 1024;

        public static string GuardarDocumento(
            HttpPostedFileBase archivo,
            int id_expediente)
        {
            if (id_expediente <= 0)
            {
                throw new ArgumentException(
                    "El expediente no es valido."
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
                    "El archivo supera el tamaño permitido de 5 MB."
                );
            }

            string extension = Path.GetExtension(
                archivo.FileName
            ).ToLowerInvariant();

            string[] extensionesPermitidas =
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            if (!extensionesPermitidas.Contains(extension))
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

            // Guardar fuera de las carpetas públicas.
            string rutaBase = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "App_Data",
                "DocumentosExpedientes"
            );

            string carpetaExpediente = Path.Combine(
                rutaBase,
                id_expediente.ToString()
            );

            Directory.CreateDirectory(carpetaExpediente);

            // Nombre interno único para evitar sobrescrituras.
            string nombreInterno =
                Guid.NewGuid().ToString("N") + extension;

            string rutaArchivo = Path.Combine(
                carpetaExpediente,
                nombreInterno
            );

            archivo.SaveAs(rutaArchivo);

            // Retornar solo el nombre interno,
            // nunca la ruta física del servidor.
            return nombreInterno;
        }
    }
}
