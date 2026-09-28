using EduNexus.Abstracciones.ModelosParaUI.Grados;
using EduNexus.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RCONF-04-001 Como administrador quiero crear y administrar los grados (nombre) para asignar las secciones
    public class GradosController : Controller
    {
        public ActionResult ListadoDeGrados()
        {
            List<GradosDto> grados = ObtenerGrados();
            return View(grados);
        }

        public ActionResult DetalleDeGrado(int? id)
        {
            List<GradosDto> grados = ObtenerGrados();
            GradosDto grado = grados.FirstOrDefault(x => x.id_grado == id);
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
        public ActionResult CrearGrado(GradosDto grado)
        {
            // Escenario 3: creación con dato obligatorio incompleto
            if (!ModelState.IsValid)
            {
                return View(grado);
            }

            lock (DatosEnMemoria.Bloqueo)
            {
                string descripcion = grado.descripcion.Trim();

                // Escenario 2: creación de grado duplicado
                if (ExisteGrado(grado.grado, descripcion, 0))
                {
                    ModelState.AddModelError("", "El grado ya está registrado.");
                    return View(grado);
                }

                // Escenario 1: creación exitosa del grado
                grado.descripcion = descripcion;
                grado.id_grado = DatosEnMemoria.Grados.Any() ? DatosEnMemoria.Grados.Max(x => x.id_grado) + 1 : 1;
                DatosEnMemoria.Grados.Add(grado);
            }

            TempData["Mensaje"] = "El grado se registró correctamente.";
            return RedirectToAction("ListadoDeGrados");
        }

        public ActionResult EditarGrado(int? id)
        {
            List<GradosDto> grados = ObtenerGrados();
            GradosDto grado = grados.FirstOrDefault(x => x.id_grado == id);
            if (grado == null)
            {
                return HttpNotFound();
            }
            return View(grado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarGrado(GradosDto grado)
        {
            if (!ModelState.IsValid)
            {
                return View(grado);
            }

            lock (DatosEnMemoria.Bloqueo)
            {
                GradosDto existente = DatosEnMemoria.Grados.FirstOrDefault(x => x.id_grado == grado.id_grado);
                if (existente == null)
                {
                    return HttpNotFound();
                }

                string descripcion = grado.descripcion.Trim();
                if (ExisteGrado(grado.grado, descripcion, grado.id_grado))
                {
                    ModelState.AddModelError("", "El grado ya está registrado.");
                    return View(grado);
                }

                // Escenario 4: las secciones guardan el id del grado,
                // por lo que el cambio se refleja en todas las secciones asociadas
                existente.grado = grado.grado;
                existente.descripcion = descripcion;
            }

            TempData["Mensaje"] = "El grado se actualizó correctamente.";
            return RedirectToAction("ListadoDeGrados");
        }

        public ActionResult EliminarGrado(int? id)
        {
            GradosDto grado = ObtenerGrados().FirstOrDefault(x => x.id_grado == id);
            if (grado == null)
            {
                return HttpNotFound();
            }
            lock (DatosEnMemoria.Bloqueo)
            {
                ViewBag.CantidadSecciones = DatosEnMemoria.Secciones.Count(x => x.grado == grado.id_grado);
            }
            return View(grado);
        }

        [HttpPost, ActionName("EliminarGrado")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEliminarGrado(int id)
        {
            lock (DatosEnMemoria.Bloqueo)
            {
                GradosDto grado = DatosEnMemoria.Grados.FirstOrDefault(x => x.id_grado == id);
                if (grado == null)
                {
                    return HttpNotFound();
                }

                // Escenario 6: intento de eliminar grado con secciones asignadas
                if (DatosEnMemoria.Secciones.Any(x => x.grado == id))
                {
                    TempData["Error"] = "No se puede eliminar el grado porque tiene secciones activas asociadas.";
                    return RedirectToAction("ListadoDeGrados");
                }

                // Escenario 5: eliminación de grado sin secciones asignadas
                DatosEnMemoria.Grados.Remove(grado);
            }

            TempData["Mensaje"] = "El grado se eliminó correctamente.";
            return RedirectToAction("ListadoDeGrados");
        }

        private bool ExisteGrado(int numero, string descripcion, int idExcluido)
        {
            return DatosEnMemoria.Grados.Any(x => x.id_grado != idExcluido
                && (x.grado == numero
                    || string.Equals(x.descripcion.Trim(), descripcion, StringComparison.OrdinalIgnoreCase)));
        }

        private List<GradosDto> ObtenerGrados()
        {
            lock (DatosEnMemoria.Bloqueo)
            {
                return DatosEnMemoria.Grados
                    .OrderBy(x => x.grado)
                    .Select(x => new GradosDto { id_grado = x.id_grado, grado = x.grado, descripcion = x.descripcion })
                    .ToList();
            }
        }
    }
}
