
using EduNexus.Abstracciones.ModelosParaUI.Expedientes;
using EduNexus.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    [AutorizarAdministrador]
    public class DocumentosExpedienteController : Controller
    {
        // =========================================
        // REXP-03-002
        // ADJUNTAR DOCUMENTOS AL EXPEDIENTE
        // =========================================

        // GET: DocumentosExpediente/AdjuntarDocumento
        [HttpGet]
        public ActionResult AdjuntarDocumento(int? id_expediente)
        {
            if (!id_expediente.HasValue ||
                id_expediente.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            DocumentoExpedienteDto modelo =
                new DocumentoExpedienteDto
                {
                    id_expediente = id_expediente.Value
                };

            return View(modelo);
        }

        // POST: DocumentosExpediente/AdjuntarDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdjuntarDocumento(
            DocumentoExpedienteDto modelo,
            HttpPostedFileBase archivo)
        {
            if (modelo == null)
            {
                return new HttpStatusCodeResult(400);
            }

            // =========================================
            // PASO 1: VALIDAR EXPEDIENTE
            // =========================================

            if (modelo.id_expediente <= 0)
            {
                ModelState.AddModelError(
                    "id_expediente",
                    "Debe indicar un expediente válido."
                );
            }

            // =========================================
            // PASO 2: VALIDAR ARCHIVO
            // =========================================

            if (archivo == null || archivo.ContentLength <= 0)
            {
                ModelState.AddModelError(
                    "archivo",
                    "Debe seleccionar un documento."
                );
            }
            else
            {
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

                if (Array.IndexOf(
                    extensionesPermitidas,
                    extension) < 0)
                {
                    ModelState.AddModelError(
                        "archivo",
                        "Solo se permiten archivos PDF, JPG, JPEG o PNG."
                    );
                }

                const int tamanoMaximo = 5 * 1024 * 1024;

                if (archivo.ContentLength > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        "archivo",
                        "El documento no puede superar los 5 MB."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // =========================================
            // PASO 3: PREPARAR DATOS DEL DOCUMENTO
            // =========================================

            modelo.nombre_archivo = Path.GetFileName(
                archivo.FileName
            );

            modelo.tipo_archivo = archivo.ContentType;

            modelo.fecha_registro = DateTime.Now;

            // =========================================
            // PASO 4: ALMACENAMIENTO PENDIENTE
            // =========================================

            // La clase AlmacenamientoDocumentoExpediente
            // ya está creada.
            //
            // Todavía no se ejecuta porque falta
            // verificar la existencia real del expediente
            // y registrar el documento de forma persistente.

            ModelState.AddModelError(
                "",
                "El documento pasó las validaciones iniciales, " +
                "pero todavía no se ha almacenado. " +
                "Está pendiente verificar el expediente " +
                "y registrar su historial documental."
            );

            return View(modelo);
        }

        // =========================================
        // REXP-03-002
        // HISTORIAL DE DOCUMENTOS
        // =========================================

        // GET: DocumentosExpediente/HistorialDocumentos
        [HttpGet]
        public ActionResult HistorialDocumentos(int? id_expediente)
        {
            if (!id_expediente.HasValue ||
                id_expediente.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            ViewBag.IdExpediente = id_expediente.Value;

            // Pendiente: consultar documentos desde MySQL.
            // Por ahora no se muestran documentos ficticios.

            List<DocumentoExpedienteDto> documentos =
                new List<DocumentoExpedienteDto>();

            return View(documentos);
        }
    }
}
