
using EduNexus.Abstracciones.ModelosParaUI.Expedientes;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // REXP-03-002: documentos adjuntos al expediente estudiantil.
    // El archivo se guarda en App_Data y su registro en la tabla documentos_expediente.
    [AutorizarAdministrador]
    public class DocumentosExpedienteController : Controller
    {
        private readonly EduNexusDbContext db = new EduNexusDbContext();

        // =========================================
        // REXP-03-002
        // ADJUNTAR DOCUMENTOS AL EXPEDIENTE
        // =========================================

        // GET: DocumentosExpediente/AdjuntarDocumento?id_expediente=5
        [HttpGet]
        public ActionResult AdjuntarDocumento(int? id_expediente)
        {
            if (!id_expediente.HasValue ||
                id_expediente.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            string estudiante;

            if (!ExpedienteExiste(id_expediente.Value, out estudiante))
            {
                return HttpNotFound();
            }

            return View(new DocumentoExpedienteDto
            {
                id_expediente = id_expediente.Value,
                estudiante = estudiante
            });
        }

        // POST: DocumentosExpediente/AdjuntarDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdjuntarDocumento(
            [Bind(Include = "id_expediente,nombre_documento,descripcion")]
            DocumentoExpedienteDto modelo,
            HttpPostedFileBase archivo)
        {
            if (modelo == null || modelo.id_expediente <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            // =========================================
            // PASO 1: VALIDAR EXPEDIENTE
            // =========================================

            string estudiante;

            if (!ExpedienteExiste(modelo.id_expediente, out estudiante))
            {
                return HttpNotFound();
            }

            modelo.estudiante = estudiante;

            // =========================================
            // PASO 2: VALIDAR DATOS Y ARCHIVO
            // =========================================

            if (modelo.nombre_documento != null &&
                string.IsNullOrWhiteSpace(modelo.nombre_documento))
            {
                ModelState.AddModelError("nombre_documento", "Debe ingresar el nombre del documento.");
            }

            string extension = "";

            if (archivo == null || archivo.ContentLength <= 0)
            {
                ModelState.AddModelError(
                    "archivo",
                    "Debe seleccionar un documento."
                );
            }
            else
            {
                extension = AlmacenamientoDocumentoExpediente.ExtensionSegura(archivo.FileName);

                if (!AlmacenamientoDocumentoExpediente.ExtensionPermitida(extension))
                {
                    ModelState.AddModelError(
                        "archivo",
                        "Solo se permiten archivos PDF, JPG, JPEG o PNG."
                    );
                }

                if (archivo.ContentLength > AlmacenamientoDocumentoExpediente.TamanoMaximo)
                {
                    ModelState.AddModelError(
                        "archivo",
                        "El documento no puede superar los " +
                        AlmacenamientoDocumentoExpediente.TamanoMaximoTexto + "."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // =========================================
            // PASO 3: GUARDAR EL ARCHIVO
            // =========================================

            string nombreInterno;

            try
            {
                nombreInterno = AlmacenamientoDocumentoExpediente.GuardarDocumento(
                    archivo,
                    modelo.id_expediente);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("archivo", ex.Message);
                return View(modelo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error guardando el archivo del expediente: " + ex);
                ModelState.AddModelError("", "No fue posible guardar el archivo. Intente de nuevo.");
                return View(modelo);
            }

            // =========================================
            // PASO 4: REGISTRAR EL DOCUMENTO EN LA BASE DE DATOS
            // Si falla, se elimina el archivo físico para no dejar huérfanos.
            // =========================================

            try
            {
                string nombreArchivo = AlmacenamientoDocumentoExpediente.NombreSeguro(archivo.FileName);

                if (nombreArchivo.Length == 0)
                {
                    nombreArchivo = nombreInterno;
                }

                var documento = new DocumentoExpedienteEntity
                {
                    fk_id_expediente = modelo.id_expediente,
                    nombre_documento = modelo.nombre_documento.Trim(),
                    descripcion = string.IsNullOrWhiteSpace(modelo.descripcion) ? null : modelo.descripcion.Trim(),
                    nombre_archivo = nombreArchivo,
                    nombre_interno = nombreInterno,
                    tipo_archivo = AlmacenamientoDocumentoExpediente.TipoMime(extension),
                    tamano_bytes = archivo.ContentLength,
                    fecha_registro = DateTime.Now,
                    fk_id_usuario = IdUsuarioActual()
                };

                db.DocumentosExpediente.Add(documento);

                Auditoria.Registrar(
                    db,
                    "Adjuntar documento al expediente",
                    "Se adjuntó el documento \"" + documento.nombre_documento + "\" al expediente #" + modelo.id_expediente + ".",
                    null,
                    new
                    {
                        expediente = documento.fk_id_expediente,
                        documento.nombre_documento,
                        documento.nombre_archivo,
                        documento.tipo_archivo,
                        documento.tamano_bytes
                    });

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error registrando documento del expediente: " + ex);

                try
                {
                    AlmacenamientoDocumentoExpediente.EliminarDocumento(modelo.id_expediente, nombreInterno);
                }
                catch (Exception exArchivo)
                {
                    System.Diagnostics.Trace.TraceError("No se pudo eliminar el archivo huérfano: " + exArchivo);
                }

                ModelState.AddModelError("", "No fue posible registrar el documento. Intente de nuevo.");
                return View(modelo);
            }

            TempData["Mensaje"] = "El documento se adjuntó correctamente al expediente.";
            return RedirectToAction("HistorialDocumentos", new { id_expediente = modelo.id_expediente });
        }

        // =========================================
        // REXP-03-002
        // HISTORIAL DE DOCUMENTOS
        // =========================================

        // GET: DocumentosExpediente/HistorialDocumentos?id_expediente=5
        [HttpGet]
        public ActionResult HistorialDocumentos(int? id_expediente)
        {
            if (!id_expediente.HasValue ||
                id_expediente.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            string estudiante;

            if (!ExpedienteExiste(id_expediente.Value, out estudiante))
            {
                return HttpNotFound();
            }

            int idExpediente = id_expediente.Value;

            ViewBag.IdExpediente = idExpediente;
            ViewBag.Estudiante = estudiante;

            List<DocumentoExpedienteDto> documentos =
                (from d in db.DocumentosExpediente
                 where d.fk_id_expediente == idExpediente
                 join u in db.Usuarios on d.fk_id_usuario equals u.id_usuario into us
                 from u in us.DefaultIfEmpty()
                 orderby d.fecha_registro descending
                 select new { d, nombre = u != null ? u.nombre : null, apellido = u != null ? u.apellido1 : null })
                .ToList()
                .Select(x => new DocumentoExpedienteDto
                {
                    id_documento = x.d.id_documento,
                    id_expediente = x.d.fk_id_expediente,
                    nombre_documento = x.d.nombre_documento,
                    descripcion = x.d.descripcion,
                    nombre_archivo = x.d.nombre_archivo,
                    tipo_archivo = x.d.tipo_archivo,
                    tamano_bytes = x.d.tamano_bytes,
                    fecha_registro = x.d.fecha_registro,
                    registrado_por = ((x.nombre ?? "") + " " + (x.apellido ?? "")).Trim()
                })
                .ToList();

            return View(documentos);
        }

        // =========================================
        // DESCARGAR DOCUMENTO
        // =========================================

        // GET: DocumentosExpediente/Descargar/7
        [HttpGet]
        public ActionResult Descargar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            int idDocumento = id.Value;

            DocumentoExpedienteEntity documento = db.DocumentosExpediente
                .FirstOrDefault(d => d.id_documento == idDocumento);

            if (documento == null)
            {
                return HttpNotFound();
            }

            // La ruta se arma con el nombre interno validado y debe quedar
            // dentro de App_Data/DocumentosExpedientes.
            string ruta = AlmacenamientoDocumentoExpediente.RutaDocumento(
                documento.fk_id_expediente,
                documento.nombre_interno);

            if (ruta == null || !System.IO.File.Exists(ruta))
            {
                TempData["Error"] = "El archivo del documento no está disponible en el servidor.";
                return RedirectToAction("HistorialDocumentos", new { id_expediente = documento.fk_id_expediente });
            }

            string tipo = AlmacenamientoDocumentoExpediente.TipoMime(
                AlmacenamientoDocumentoExpediente.ExtensionSegura(documento.nombre_interno));

            return File(ruta, tipo, documento.nombre_archivo);
        }

        // =========================================
        // MÉTODOS AUXILIARES
        // =========================================

        private bool ExpedienteExiste(int idExpediente, out string estudiante)
        {
            var datos = (from e in db.Expedientes
                         where e.id_expediente == idExpediente
                         join p in db.Prematriculas on e.fk_id_matricula equals p.id_prematricula into ps
                         from p in ps.DefaultIfEmpty()
                         select new
                         {
                             e.id_expediente,
                             nombre = p != null ? p.nombre : null,
                             apellido1 = p != null ? p.apellido1 : null,
                             apellido2 = p != null ? p.apellido2 : null
                         })
                .FirstOrDefault();

            estudiante = datos == null
                ? null
                : string.Join(" ", new[] { datos.nombre, datos.apellido1, datos.apellido2 }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            return datos != null;
        }

        private string IdUsuarioActual()
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion]
                as EduNexus.Abstracciones.ModelosParaUI.Usuarios.UsuarioDto;

            return usuario != null ? usuario.id_usuario : null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
