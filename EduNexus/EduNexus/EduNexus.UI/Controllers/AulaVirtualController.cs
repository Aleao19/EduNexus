using EduNexus.Abstracciones.ModelosParaUI.Asignaciones;
using EduNexus.Abstracciones.ModelosParaUI.Materiales;
using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // Aula virtual: Docente (sus clases), Estudiante (las de sus secciones),
    // Administrador y Director (todas, solo consulta).
    [AutorizarRoles(RolesSistema.Administrador, RolesSistema.Director, RolesSistema.Docente, RolesSistema.Estudiante)]
    public class AulaVirtualController : Controller
    {
        // GET: AulaVirtual
        public ActionResult AulaVirtual()
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                List<SeccionMateriaEntity> clases = ObtenerClasesVisibles(db, usuario);

                Dictionary<int, string> secciones = HorarioServicio.EtiquetasDeSecciones(db);

                List<int> idsMaterias = clases.Select(c => c.fk_id_materia).Distinct().ToList();
                Dictionary<int, MateriaEntity> materias = db.Materias
                    .Where(m => idsMaterias.Contains(m.id_materia)).ToList()
                    .ToDictionary(m => m.id_materia);

                List<string> idsProfesores = clases.Select(c => c.fk_id_profesor).Distinct().ToList();
                Dictionary<string, UsuarioEntity> profesores = db.Usuarios
                    .Where(u => idsProfesores.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                List<MateriasDto> modelo = clases.Where(c => materias.ContainsKey(c.fk_id_materia)).Select(c =>
                {
                    MateriaEntity materia = materias[c.fk_id_materia];
                    UsuarioEntity profesor;
                    if (c.fk_id_profesor == null || !profesores.TryGetValue(c.fk_id_profesor, out profesor)) profesor = null;
                    string seccion;
                    secciones.TryGetValue(c.fk_id_seccion, out seccion);

                    return new MateriasDto
                    {
                        id_materia = materia.id_materia,
                        nombre = materia.nombre,
                        profesor = profesor != null ? HorarioServicio.NombreCompleto(profesor) : "",
                        id_seccion = c.fk_id_seccion,
                        seccion = seccion
                    };
                })
                .OrderBy(x => x.nombre).ThenBy(x => x.seccion)
                .ToList();

                return View(modelo);
            }
        }

        // Clases (materia + sección) del periodo actual que el usuario puede ver:
        // - Docente: las clases donde él es el docente (secciones_materias.fk_id_profesor).
        // - Estudiante: las materias de las secciones donde está matriculado.
        // - Administrador y Director: todas (solo consulta).
        // - Cualquier otro rol: ninguna.
        internal static List<SeccionMateriaEntity> ObtenerClasesVisibles(EduNexusDbContext db, UsuarioDto usuario)
        {
            bool esDocente = RolesSistema.Es(usuario, RolesSistema.Docente);
            bool esEstudiante = RolesSistema.Es(usuario, RolesSistema.Estudiante);
            bool veTodas = RolesSistema.Es(usuario, RolesSistema.Administrador, RolesSistema.Director);

            if (!esDocente && !esEstudiante && !veTodas)
            {
                return new List<SeccionMateriaEntity>();
            }

            DateTime hoy = DateTime.Today;

            List<int> idsCalendario = db.Calendarios
                .Where(c => c.fecha_inicio <= hoy && c.fecha_fin >= hoy)
                .Select(c => c.id_calendario).ToList();

            List<int> idsSecciones = db.Secciones
                .Where(s => idsCalendario.Contains(s.fk_id_calendario))
                .Select(s => s.id_seccion).ToList();

            IQueryable<SeccionMateriaEntity> consulta = db.SeccionesMaterias
                .Where(sm => idsSecciones.Contains(sm.fk_id_seccion));

            string idUsuario = usuario.id_usuario;

            if (esDocente)
            {
                consulta = consulta.Where(sm => sm.fk_id_profesor == idUsuario);
            }
            else if (esEstudiante)
            {
                List<int> susSecciones = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_usuario == idUsuario)
                    .Select(se => se.fk_id_seccion).ToList();

                consulta = consulta.Where(sm => susSecciones.Contains(sm.fk_id_seccion));
            }

            return consulta.ToList();
        }

        // GET: AulaVirtual/Materia/5?seccion=2
        public ActionResult Materia(int? id, int? seccion)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                SeccionMateriaEntity clase = id.HasValue
                    ? ObtenerClasesVisibles(db, usuario).FirstOrDefault(c =>
                        c.fk_id_materia == id.Value && (!seccion.HasValue || c.fk_id_seccion == seccion.Value))
                    : null;

                // RAV-12-005 escenario 5: acceso restringido
                if (clase == null)
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual");
                }

                int idSeccion = clase.fk_id_seccion;
                int idMateria = clase.fk_id_materia;
                bool esEstudiante = RolesSistema.Es(usuario, RolesSistema.Estudiante);

                MateriaEntity materia = db.Materias.FirstOrDefault(m => m.id_materia == idMateria);
                string etiquetaSeccion;
                HorarioServicio.EtiquetasDeSecciones(db).TryGetValue(idSeccion, out etiquetaSeccion);

                // Estudiantes reales de la sección
                List<string> idsEstudiantes = db.SeccionesEstudiantes
                    .Where(se => se.fk_id_seccion == idSeccion)
                    .Select(se => se.fk_id_usuario).ToList();
                Dictionary<string, UsuarioEntity> estudiantes = db.Usuarios
                    .Where(u => idsEstudiantes.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                // Asignaciones reales de la clase
                List<AsignacionEntity> filas = db.Asignaciones
                    .Where(a => a.fk_id_seccion == idSeccion && a.fk_id_materia == idMateria)
                    .ToList();

                List<AsignacionEntity> registros;
                var resumenPorAsignacion = new Dictionary<int, ResumenAsignacionVm>();

                if (esEstudiante)
                {
                    // Estudiante: solo sus filas
                    string idUsuario = usuario.id_usuario;
                    registros = filas
                        .Where(a => a.fk_id_usuario == idUsuario)
                        .OrderByDescending(a => a.fecha_apertura ?? DateTime.MinValue)
                        .ThenBy(a => a.nombre)
                        .ToList();
                }
                else
                {
                    // Docente, Director y Administrador: una tarjeta por asignación (filas unidas por id_grupo)
                    var grupos = filas
                        .GroupBy(a => !string.IsNullOrEmpty(a.id_grupo) ? a.id_grupo : "n:" + a.nombre)
                        .ToList();

                    registros = grupos
                        .Select(g => g.First())
                        .OrderByDescending(a => a.fecha_apertura ?? DateTime.MinValue)
                        .ThenBy(a => a.nombre)
                        .ToList();

                    foreach (var g in grupos)
                    {
                        resumenPorAsignacion[g.First().id_asignacion] = new ResumenAsignacionVm
                        {
                            total = g.Count(),
                            entregadas = g.Count(a => AsignacionesController.HayEntrega(a)),
                            calificadas = g.Count(a => AsignacionesController.EsCalificada(a))
                        };
                    }
                }

                List<AsignacionesDto> asignaciones = registros.Select(a => new AsignacionesDto
                {
                    id_asignacion = a.id_asignacion,
                    nombre = a.nombre,
                    descripcion = a.descripcion,
                    porcentaje_nota = (float)a.porcentaje_nota,
                    calificacion = (float)a.calificacion,
                    estado = esEstudiante ? AsignacionesController.EstadoVisible(a) : "Publicada",
                    fechaDeApertura = a.fecha_apertura ?? DateTime.MinValue,
                    fechaDeCierre = a.fecha_cierre ?? DateTime.MinValue,
                    fechaDeEntrega = a.fecha_entrega ?? DateTime.MinValue
                }).ToList();

                // RAV-12-004 escenario 5: promedio con las notas registradas
                // (estudiante: sus notas; docente/director/admin: todas las de la clase)
                IEnumerable<AsignacionEntity> paraPromedio = esEstudiante
                    ? filas.Where(a => a.fk_id_usuario == usuario.id_usuario)
                    : filas;
                List<AsignacionEntity> conNota = paraPromedio.Where(a => AsignacionesController.EsCalificada(a)).ToList();
                ViewBag.PromedioClase = conNota.Any() ? (double?)conNota.Average(a => (double)a.calificacion) : null;

                ViewBag.IdMateria = idMateria;
                ViewBag.IdSeccion = idSeccion;
                ViewBag.NombreMateria = materia != null ? materia.nombre : "Materia";
                ViewBag.NombreSeccion = etiquetaSeccion ?? "";
                ViewBag.EsDocente = RolesSistema.Es(usuario, RolesSistema.Docente);
                ViewBag.EsEstudiante = esEstudiante;
                ViewBag.ResumenPorAsignacion = resumenPorAsignacion;
                ViewBag.Estudiantes = estudiantes.Values
                    .Select(u => HorarioServicio.NombreCompleto(u))
                    .OrderBy(n => n)
                    .ToList();

                // Material publicado de la clase
                List<MaterialClaseEntity> materiales = db.MaterialesClase
                    .Where(m => m.fk_id_seccion == idSeccion && m.fk_id_materia == idMateria)
                    .OrderByDescending(m => m.fecha_publicacion)
                    .ToList();

                List<string> idsPublicadores = materiales.Select(m => m.fk_id_publicado_por).Distinct().ToList();
                Dictionary<string, UsuarioEntity> publicadores = db.Usuarios
                    .Where(u => idsPublicadores.Contains(u.id_usuario)).ToList()
                    .ToDictionary(u => u.id_usuario);

                ViewBag.Materiales = materiales.Select(m =>
                {
                    UsuarioEntity publicador;
                    publicadores.TryGetValue(m.fk_id_publicado_por, out publicador);

                    return new MaterialDto
                    {
                        id_material = m.id_material,
                        nombre = m.nombre,
                        tipo = m.tipo,
                        url = m.url,
                        nombre_archivo = m.nombre_archivo,
                        fecha_publicacion = m.fecha_publicacion,
                        publicado_por = publicador != null ? HorarioServicio.NombreCompleto(publicador) : ""
                    };
                }).ToList();

                return View(asignaciones);
            }
        }
        // RAV-12-001: publicar material (archivo o enlace) en la clase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PublicarMaterial(int idSeccion, int idMateria, string nombre, string tipo, string url, HttpPostedFileBase archivo)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                // Escenario 5: solo el docente de esa clase puede publicar
                bool esDocenteDeLaClase = RolesSistema.Es(usuario, RolesSistema.Docente)
                    && ObtenerClasesVisibles(db, usuario).Any(c => c.fk_id_seccion == idSeccion && c.fk_id_materia == idMateria);

                if (!esDocenteDeLaClase)
                {
                    return Json(new { ok = false, mensaje = "No tiene acceso a esta materia." });
                }

                var errores = new Dictionary<string, string>();

                nombre = (nombre ?? "").Trim();
                url = (url ?? "").Trim();

                // Escenario 3: datos incompletos
                if (nombre.Length == 0)
                {
                    errores["nombre"] = "El nombre es obligatorio.";
                }
                else if (nombre.Length > 150)
                {
                    errores["nombre"] = "El nombre no puede superar 150 caracteres.";
                }

                if (tipo != "Archivo" && tipo != "Enlace")
                {
                    errores["tipo"] = "Seleccione el tipo de material.";
                }
                else if (tipo == "Archivo")
                {
                    if (archivo == null || archivo.ContentLength <= 0)
                    {
                        errores["archivo"] = "Debe adjuntar un archivo.";
                    }
                    else
                    {
                        // Escenario 4: archivo no permitido
                        string errorArchivo = AlmacenamientoMaterial.Validar(archivo);
                        if (errorArchivo != null)
                        {
                            errores["archivo"] = errorArchivo;
                        }
                    }
                }
                else
                {
                    Uri uri;
                    if (url.Length == 0)
                    {
                        errores["url"] = "La URL es obligatoria.";
                    }
                    else if (url.Length > 500 || !Uri.TryCreate(url, UriKind.Absolute, out uri)
                        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        errores["url"] = "Escriba una URL válida que comience con http:// o https://";
                    }
                }

                if (errores.Count > 0)
                {
                    return Json(new { ok = false, errores = errores });
                }

                // Escenarios 1 y 2: se publica el material
                var material = new MaterialClaseEntity
                {
                    fk_id_seccion = idSeccion,
                    fk_id_materia = idMateria,
                    nombre = nombre,
                    tipo = tipo,
                    fecha_publicacion = DateTime.Now,
                    fk_id_publicado_por = usuario.id_usuario
                };

                if (tipo == "Archivo")
                {
                    material.nombre_archivo = AlmacenamientoDocumentoExpediente.NombreSeguro(archivo.FileName);
                    material.nombre_interno = AlmacenamientoMaterial.Guardar(archivo, idSeccion, idMateria);
                }
                else
                {
                    material.url = url;
                }

                try
                {
                    db.MaterialesClase.Add(material);
                    db.SaveChanges();
                }
                catch
                {
                    if (material.nombre_interno != null)
                    {
                        AlmacenamientoMaterial.Eliminar(idSeccion, idMateria, material.nombre_interno);
                    }
                    throw;
                }
            }

            TempData["Mensaje"] = "El material se publicó correctamente.";
            return Json(new { ok = true });
        }

        // Descarga de un archivo publicado (docente de la clase, estudiantes de la sección, director y administrador)
        public ActionResult DescargarMaterial(int? id)
        {
            var usuario = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            using (var db = new EduNexusDbContext())
            {
                MaterialClaseEntity material = id.HasValue
                    ? db.MaterialesClase.FirstOrDefault(m => m.id_material == id.Value)
                    : null;

                // Escenario 3: el material ya no existe
                if (material == null || material.tipo != "Archivo")
                {
                    TempData["Error"] = "El material no está disponible.";
                    return RedirectToAction("AulaVirtual");
                }

                // Escenario 4: sin acceso a la materia
                bool tieneAcceso = ObtenerClasesVisibles(db, usuario)
                    .Any(c => c.fk_id_seccion == material.fk_id_seccion && c.fk_id_materia == material.fk_id_materia);

                if (!tieneAcceso)
                {
                    TempData["Error"] = "No tiene acceso a esta materia.";
                    return RedirectToAction("AulaVirtual");
                }

                // Escenario 3: el archivo fue eliminado del servidor
                string ruta = AlmacenamientoMaterial.RutaArchivo(material.fk_id_seccion, material.fk_id_materia, material.nombre_interno);
                if (ruta == null || !System.IO.File.Exists(ruta))
                {
                    TempData["Error"] = "El material no está disponible.";
                    return RedirectToAction("Materia", new { id = material.fk_id_materia, seccion = material.fk_id_seccion });
                }

                // Escenario 1: se descarga con su nombre original
                string extension = AlmacenamientoDocumentoExpediente.ExtensionSegura(material.nombre_archivo);
                return File(ruta, AlmacenamientoMaterial.TipoMime(extension), material.nombre_archivo);
            }
        }
    }
}