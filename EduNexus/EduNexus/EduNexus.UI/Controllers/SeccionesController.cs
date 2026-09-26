using EduNexus.Abstracciones.ModelosParaUI.Grados;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using EduNexus.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RCONF-04-002 Como administrador quiero crear las secciones con nombre del grado, nombre de sección y año para asignar a los estudiantes
    public class SeccionesController : Controller
    {
        // GET: Secciones
        public ActionResult ListadoDeSecciones()
        {
            List<SeccionesDto> secciones = ObtenerSecciones();
            return View(secciones);
        }

        // RGRU-06-003 Como administrador quiero consultar la cantidad de estudiantes por grupo para controlar los cupos disponibles
        // GET: Secciones/CuposPorGrupo?grado=1&anio=2026
        public ActionResult CuposPorGrupo(int? grado, int? anio)
        {
            List<SeccionesDto> secciones = ObtenerSecciones();

            ViewBag.Grados = ObtenerGrados();
            ViewBag.Anios = secciones.Where(x => x.anio.HasValue)
                .Select(x => x.anio.Value)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();
            ViewBag.GradoSeleccionado = grado;
            ViewBag.AnioSeleccionado = anio;

            // Escenario 3: filtrado de grupos por nivel educativo o periodo lectivo
            if (grado.HasValue)
            {
                secciones = secciones.Where(x => x.grado == grado.Value).ToList();
            }
            if (anio.HasValue)
            {
                secciones = secciones.Where(x => x.anio == anio.Value).ToList();
            }

            // Escenarios 1 y 2: cada grupo muestra estudiantes asignados y cupos disponibles
            // (un grupo sin estudiantes muestra 0 y el cupo completo disponible)
            return View(secciones);
        }

        // GET: Secciones/Details/5
        public ActionResult DetalleDeSeccion(int? id)
        {
            List<SeccionesDto> secciones = ObtenerSecciones();
            SeccionesDto seccion = secciones.FirstOrDefault(x => x.id_seccion == id);
            if (seccion == null)
            {
                return HttpNotFound();
            }
            return View(seccion);
        }

        // GET: Secciones/Create
        public ActionResult CrearSeccion()
        {
            ViewBag.Grados = ObtenerGrados();
            return View();
        }

        // POST: Secciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearSeccion(SeccionesDto seccion)
        {
            ViewBag.Grados = ObtenerGrados();

            // Escenario 3: creación con datos obligatorios incompletos
            if (!ModelState.IsValid)
            {
                return View(seccion);
            }

            lock (DatosEnMemoria.Bloqueo)
            {
                string nombreGrado = seccion.nombre_grado.Trim();
                string nombre = seccion.nombre.Trim();

                // Escenario 4: creación de sección para un grado inexistente
                GradosDto grado = DatosEnMemoria.Grados.FirstOrDefault(x =>
                    string.Equals(x.nombre.Trim(), nombreGrado, StringComparison.OrdinalIgnoreCase));
                if (grado == null)
                {
                    ModelState.AddModelError("nombre_grado", "El grado no existe en el sistema. Primero debe crear el grado.");
                    return View(seccion);
                }

                // Escenario 2: creación de sección duplicada (mismo nombre, grado y año)
                if (ExisteSeccion(nombre, grado.id_grado, seccion.anio.Value, 0))
                {
                    ModelState.AddModelError("", "Ya existe esa sección para ese grado y año.");
                    return View(seccion);
                }

                // Escenario 1: creación exitosa de sección
                seccion.id_seccion = DatosEnMemoria.Secciones.Any() ? DatosEnMemoria.Secciones.Max(x => x.id_seccion) + 1 : 1;
                seccion.nombre = nombre;
                seccion.grado = grado.id_grado;
                seccion.nombre_grado = null;
                seccion.estudiantes_matriculados = 0;
                DatosEnMemoria.Secciones.Add(seccion);
            }

            TempData["Mensaje"] = "La sección se registró correctamente y está disponible para asignar estudiantes.";
            return RedirectToAction("ListadoDeSecciones");
        }

        // GET: Secciones/Edit/5
        public ActionResult EditarSeccion(int? id)
        {
            List<SeccionesDto> secciones = ObtenerSecciones();
            SeccionesDto seccion = secciones.FirstOrDefault(x => x.id_seccion == id);
            if (seccion == null)
            {
                return HttpNotFound();
            }
            return View(seccion);
        }

        // POST: Secciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarSeccion(SeccionesDto seccion)
        {
            lock (DatosEnMemoria.Bloqueo)
            {
                SeccionesDto existente = DatosEnMemoria.Secciones.FirstOrDefault(x => x.id_seccion == seccion.id_seccion);
                if (existente == null)
                {
                    return HttpNotFound();
                }

                // El grado no se modifica desde esta pantalla
                seccion.grado = existente.grado;
                seccion.nombre_grado = NombreDelGrado(existente.grado);
                ModelState.Remove("nombre_grado");

                if (!ModelState.IsValid)
                {
                    return View(seccion);
                }

                string nombre = seccion.nombre.Trim();
                if (ExisteSeccion(nombre, existente.grado, seccion.anio.Value, existente.id_seccion))
                {
                    ModelState.AddModelError("", "Ya existe esa sección para ese grado y año.");
                    return View(seccion);
                }

                // Escenario 5: edición exitosa, se registra fecha y usuario que realizó la modificación
                existente.nombre = nombre;
                existente.anio = seccion.anio;
                existente.cupo = seccion.cupo;
                existente.fecha_modificacion = DateTime.Now;
                existente.usuario_modificacion = ObtenerUsuarioActual();
            }

            TempData["Mensaje"] = "La sección se actualizó correctamente.";
            return RedirectToAction("ListadoDeSecciones");
        }

        // GET: Secciones/EliminarSeccion/5
        public ActionResult EliminarSeccion(int? id)
        {
            SeccionesDto seccion = ObtenerSecciones().FirstOrDefault(x => x.id_seccion == id);
            if (seccion == null)
            {
                return HttpNotFound();
            }
            return View(seccion);
        }

        // POST: Secciones/EliminarSeccion/5
        [HttpPost, ActionName("EliminarSeccion")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEliminarSeccion(int id)
        {
            lock (DatosEnMemoria.Bloqueo)
            {
                SeccionesDto seccion = DatosEnMemoria.Secciones.FirstOrDefault(x => x.id_seccion == id);
                if (seccion == null)
                {
                    return HttpNotFound();
                }

                // Escenario 7: intento de eliminar sección con estudiantes asignados
                if (seccion.estudiantes_matriculados > 0)
                {
                    TempData["Error"] = "No se puede eliminar la sección porque tiene estudiantes asignados.";
                    return RedirectToAction("ListadoDeSecciones");
                }

                // Escenario 6: eliminación de sección sin estudiantes
                DatosEnMemoria.Secciones.Remove(seccion);
            }

            TempData["Mensaje"] = "La sección se eliminó correctamente.";
            return RedirectToAction("ListadoDeSecciones");
        }

        private bool ExisteSeccion(string nombre, int idGrado, int anio, int idExcluido)
        {
            return DatosEnMemoria.Secciones.Any(x => x.id_seccion != idExcluido
                && x.grado == idGrado
                && x.anio == anio
                && string.Equals(x.nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase));
        }

        private string NombreDelGrado(int idGrado)
        {
            GradosDto grado = DatosEnMemoria.Grados.FirstOrDefault(x => x.id_grado == idGrado);
            return grado != null ? grado.nombre : "";
        }

        private string ObtenerUsuarioActual()
        {
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                return User.Identity.Name;
            }
            return "Administrador";
        }

        private List<GradosDto> ObtenerGrados()
        {
            lock (DatosEnMemoria.Bloqueo)
            {
                return DatosEnMemoria.Grados
                    .Select(x => new GradosDto { id_grado = x.id_grado, nombre = x.nombre })
                    .ToList();
            }
        }

        private List<SeccionesDto> ObtenerSecciones()
        {
            lock (DatosEnMemoria.Bloqueo)
            {
                return DatosEnMemoria.Secciones
                    .Select(x => new SeccionesDto
                    {
                        id_seccion = x.id_seccion,
                        nombre = x.nombre,
                        grado = x.grado,
                        nombre_grado = NombreDelGrado(x.grado),
                        anio = x.anio,
                        cupo = x.cupo,
                        estudiantes_matriculados = x.estudiantes_matriculados,
                        fecha_modificacion = x.fecha_modificacion,
                        usuario_modificacion = x.usuario_modificacion
                    })
                    .ToList();
            }
        }
    }
}
