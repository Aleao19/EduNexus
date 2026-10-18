
using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using EduNexus.Abstracciones.ModelosParaUI.Prematriculas;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;

namespace EduNexus.UI.Controllers
{
    [AutorizarAdministrador]
    public class PrematriculasController : Controller
    {
        private readonly EduNexusDbContext db = new EduNexusDbContext();

        // Muestra el formulario para registrar una prematrícula.
        [HttpGet]
        public ActionResult Crear()
        {
            CargarGrados();
            return View(new PrematriculaDto());
        }

        // Procesa el formulario y registra la prematrícula.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(PrematriculaDto modelo)
        {
            if (!ModelState.IsValid)
            {
                CargarGrados();
                return View(modelo);
            }

            if (!modelo.fecha_nacimiento.HasValue)
            {
                ModelState.AddModelError(
                    "fecha_nacimiento",
                    "La fecha de nacimiento es obligatoria."
                );

                CargarGrados();
                return View(modelo);
            }

            try
            {
                // Evita registrar dos prematrículas con la misma identificación.
                if (db.Prematriculas.Any(
                    p => p.identificacion == modelo.identificacion))
                {
                    ModelState.AddModelError(
                        "identificacion",
                        "Ya existe una prematrícula con esta identificación."
                    );

                    CargarGrados();
                    return View(modelo);
                }

                using (var transaccion = db.Database.BeginTransaction())
                {
                    var prematricula = new PrematriculaEntity
                    {
                        identificacion = modelo.identificacion,
                        nombre = modelo.nombre,
                        apellido1 = modelo.apellido1,
                        apellido2 = modelo.apellido2,
                        genero = modelo.genero,
                        fk_id_grado = modelo.grado,
                        telefono = modelo.telefono,
                        fecha_nacimiento = modelo.fecha_nacimiento.Value,
                        tiene_adecuacion = modelo.tiene_adecuacion,
                        tiene_discapacidad = modelo.tiene_discapacidad,
                        contactoEmergencia = modelo.contactoEmergencia,
                        direccion = modelo.direccion,
                        es_nuevo_ingreso = modelo.esNuevoIngreso,
                        fecha_prematricula = DateTime.Now
                    };

                    db.Prematriculas.Add(prematricula);
                    db.SaveChanges();

                    // Para estudiantes nuevos, intenta crear su expediente.
                    // PENDIENTE: verificar en Aiven si la relación real
                    // utiliza fk_id_prematricula y estos mismos campos.
                    if (modelo.esNuevoIngreso)
                    {
                        var expediente = new ExpedienteEntity
                        {
                            fk_id_prematricula = prematricula.id_prematricula,
                            identificacion = modelo.identificacion,
                            nombre = modelo.nombre,
                            apellido1 = modelo.apellido1,
                            apellido2 = modelo.apellido2,
                            genero = modelo.genero,
                            fk_id_grado = modelo.grado,
                            telefono = modelo.telefono,
                            fecha_nacimiento = modelo.fecha_nacimiento.Value,
                            tiene_adecuacion = modelo.tiene_adecuacion,
                            tiene_discapacidad = modelo.tiene_discapacidad,
                            contactoEmergencia = modelo.contactoEmergencia,
                            direccion = modelo.direccion
                        };

                        db.Expedientes.Add(expediente);
                        db.SaveChanges();
                    }

                    // Confirma ambas operaciones juntas.
                    transaccion.Commit();
                }

                TempData["Mensaje"] = modelo.esNuevoIngreso
                    ? "Prematrícula y expediente registrados correctamente."
                    : "Prematrícula registrada correctamente.";

                return RedirectToAction("Crear");
            }
            catch (Exception ex)
            {
                // Registra el detalle técnico para poder diagnosticarlo después.
                Trace.TraceError(
                    "Error en RMPR-08-001 al registrar una prematrícula: " + ex
                );

                ModelState.AddModelError(
                    "",
                    "No fue posible registrar la información. " +
                    "Verifique la conexión a Aiven y la estructura de las tablas."
                );

                CargarGrados();
                return View(modelo);
            }
        }

        // Carga los grados disponibles para la lista desplegable.
        private void CargarGrados()
        {
            using (var contexto = new EduNexusDbContext())
            {
                ViewBag.Grados = contexto.Grados
                    .OrderBy(g => g.nivel)
                    .ToList()
                    .Select(g => new SelectListItem
                    {
                        Value = g.id_grado.ToString(),
                        Text = g.nombre
                    })
                    .ToList();
            }
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
