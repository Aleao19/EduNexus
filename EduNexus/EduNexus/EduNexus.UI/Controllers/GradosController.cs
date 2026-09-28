using EduNexus.Abstracciones.ModelosParaUI.Grados;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RCONF-04-001 Como administrador quiero crear y administrar los grados (nombre) para asignar las secciones
    // Tabla grados: nombre (Descripción) y nivel (Grado)
    [AutorizarAdministrador]
    public class GradosController : Controller
    {
        public ActionResult ListadoDeGrados()
        {
            using (var db = new EduNexusDbContext())
            {
                List<GradosDto> grados = db.Grados
                    .OrderBy(g => g.nivel)
                    .ToList()
                    .Select(AGradoDto)
                    .ToList();
                return View(grados);
            }
        }

        public ActionResult DetalleDeGrado(int? id)
        {
            GradosDto grado = BuscarGrado(id);
            if (grado == null)
            {
                return HttpNotFound();
            }
            return View(grado);
        }

        public ActionResult CrearGrado()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearGrado(GradosDto modelo)
        {
            // Escenario 3: creación con dato obligatorio incompleto
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (var db = new EduNexusDbContext())
            {
                string nombre = modelo.descripcion.Trim();

                // Escenario 2: creación de grado duplicado
                if (ExisteGrado(db, modelo.grado, nombre, 0))
                {
                    ModelState.AddModelError("", "El grado ya está registrado.");
                    return View(modelo);
                }

                // Escenario 1: creación exitosa del grado
                db.Grados.Add(new GradoEntity { nivel = modelo.grado, nombre = nombre });
                db.SaveChanges();
            }

            TempData["Mensaje"] = "El grado se registró correctamente.";
            return RedirectToAction("ListadoDeGrados");
        }

        public ActionResult EditarGrado(int? id)
        {
            GradosDto grado = BuscarGrado(id);
            if (grado == null)
            {
                return HttpNotFound();
            }
            return View(grado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarGrado(GradosDto modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (var db = new EduNexusDbContext())
            {
                GradoEntity existente = db.Grados.FirstOrDefault(x => x.id_grado == modelo.id_grado);
                if (existente == null)
                {
                    return HttpNotFound();
                }

                string nombre = modelo.descripcion.Trim();
                if (ExisteGrado(db, modelo.grado, nombre, modelo.id_grado))
                {
                    ModelState.AddModelError("", "El grado ya está registrado.");
                    return View(modelo);
                }

                // Escenario 4: las secciones guardan el id del grado (fk_id_grado),
                // por lo que el cambio se refleja en todas las secciones asociadas
                existente.nivel = modelo.grado;
                existente.nombre = nombre;
                db.SaveChanges();
            }

            TempData["Mensaje"] = "El grado se actualizó correctamente.";
            return RedirectToAction("ListadoDeGrados");
        }

        public ActionResult EliminarGrado(int? id)
        {
            GradosDto grado = BuscarGrado(id);
            if (grado == null)
            {
                return HttpNotFound();
            }
            using (var db = new EduNexusDbContext())
            {
                ViewBag.CantidadSecciones = db.Secciones.Count(x => x.fk_id_grado == grado.id_grado);
            }
            return View(grado);
        }

        [HttpPost, ActionName("EliminarGrado")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEliminarGrado(int id)
        {
            using (var db = new EduNexusDbContext())
            {
                GradoEntity grado = db.Grados.FirstOrDefault(x => x.id_grado == id);
                if (grado == null)
                {
                    return HttpNotFound();
                }

                // Escenario 6: intento de eliminar grado con secciones asignadas
                if (db.Secciones.Any(x => x.fk_id_grado == id))
                {
                    TempData["Error"] = "No se puede eliminar el grado porque tiene secciones activas asociadas.";
                    return RedirectToAction("ListadoDeGrados");
                }

                // Tampoco se puede eliminar si hay estudiantes prematriculados en ese grado
                int prematriculas = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM prematricula WHERE fk_id_grado = @p0", id).First();
                if (prematriculas > 0)
                {
                    TempData["Error"] = "No se puede eliminar el grado porque tiene estudiantes asociados.";
                    return RedirectToAction("ListadoDeGrados");
                }

                // Escenario 5: eliminación de grado sin secciones asignadas
                db.Grados.Remove(grado);
                db.SaveChanges();
            }

            TempData["Mensaje"] = "El grado se eliminó correctamente.";
            return RedirectToAction("ListadoDeGrados");
        }

        private static bool ExisteGrado(EduNexusDbContext db, int nivel, string nombre, int idExcluido)
        {
            return db.Grados.Where(x => x.id_grado != idExcluido).ToList()
                .Any(x => x.nivel == nivel
                    || string.Equals(x.nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase));
        }

        private static GradosDto BuscarGrado(int? id)
        {
            if (id == null)
            {
                return null;
            }
            using (var db = new EduNexusDbContext())
            {
                GradoEntity g = db.Grados.FirstOrDefault(x => x.id_grado == id.Value);
                return g == null ? null : AGradoDto(g);
            }
        }

        private static GradosDto AGradoDto(GradoEntity g)
        {
            return new GradosDto { id_grado = g.id_grado, grado = g.nivel, descripcion = g.nombre };
        }
    }
}
