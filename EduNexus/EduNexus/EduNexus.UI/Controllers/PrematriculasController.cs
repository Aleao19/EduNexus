
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

using EduNexus.Abstracciones.ModelosParaUI.Prematriculas;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;

namespace EduNexus.UI.Controllers
{
    public class PrematriculasController : Controller
    {
        private readonly EduNexusDbContext db =
            new EduNexusDbContext();

        // ==========================================
        // RMPR-08-001 / RMPR-08-005
        // REGISTRAR PREMATRÍCULA
        // ==========================================

        [HttpGet]
        public ActionResult Crear()
        {
            if (!EsEncargado())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Acceso reservado al rol Encargado.");
            }

            CargarGrados();

            return View(new PrematriculaDto
            {
                estado = "Pendiente",
                esNuevoIngreso = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(PrematriculaDto modelo)
        {
            if (!EsEncargado())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Acceso reservado al rol Encargado.");
            }

            if (modelo == null)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Solicitud inválida.");
            }

            modelo.esNuevoIngreso = true;

            if (!ModelState.IsValid)
            {
                CargarGrados();
                return View(modelo);
            }

            try
            {
                if (!GradoExiste(modelo.grado))
                {
                    ModelState.AddModelError(
                        "grado",
                        "El grado seleccionado no existe.");

                    CargarGrados();
                    return View(modelo);
                }

                var calendario =
                    ObtenerCalendarioVigente();

                if (calendario == null)
                {
                    ModelState.AddModelError(
                        "",
                        "No existe un período lectivo vigente.");

                    CargarGrados();
                    return View(modelo);
                }

                string identificacion =
                    modelo.identificacion.Trim();

                bool solicitudPendiente =
                    db.Prematriculas.Any(p =>
                        p.identificacion == identificacion &&
                        p.estado == "Pendiente");

                if (solicitudPendiente)
                {
                    ModelState.AddModelError(
                        "identificacion",
                        "Ya existe una solicitud pendiente para este estudiante.");

                    CargarGrados();
                    return View(modelo);
                }

                bool estudianteRegistrado =
                    db.Prematriculas.Any(p =>
                        p.identificacion == identificacion &&
                        p.estado == "Aprobada");

                if (estudianteRegistrado)
                {
                    ModelState.AddModelError(
                        "identificacion",
                        "El estudiante ya tiene una prematrícula aprobada. Utilice el proceso de renovación.");

                    CargarGrados();
                    return View(modelo);
                }

                using (var transaccion =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    bool duplicada =
                        db.Prematriculas.Any(p =>
                            p.identificacion ==
                                identificacion &&
                            (p.estado == "Pendiente" ||
                             p.estado == "Aprobada"));

                    if (duplicada)
                    {
                        ModelState.AddModelError(
                            "identificacion",
                            "El estudiante ya tiene una solicitud registrada.");

                        CargarGrados();
                        return View(modelo);
                    }

                    var prematricula =
                        new PrematriculaEntity
                        {
                            identificacion = identificacion,
                            nombre = modelo.nombre.Trim(),
                            apellido1 = modelo.apellido1.Trim(),
                            apellido2 = modelo.apellido2,
                            genero = modelo.genero,
                            fk_id_grado = modelo.grado,
                            telefono = modelo.telefono,
                            fecha_nacimiento =
                                modelo.fecha_nacimiento.Value,
                            tiene_adecuacion =
                                modelo.tiene_adecuacion,
                            tiene_discapacidad =
                                modelo.tiene_discapacidad,
                            contactoEmergencia =
                                modelo.contactoEmergencia,
                            direccion = modelo.direccion,
                            es_nuevo_ingreso = true,
                            fecha_prematricula = DateTime.Now,
                            estado = "Pendiente",
                            fk_id_calendario =
                                calendario.id_calendario
                        };

                    db.Prematriculas.Add(prematricula);
                    db.SaveChanges();

                    transaccion.Commit();
                }

                TempData["Mensaje"] =
                    "La prematrícula se registró correctamente y quedó pendiente de aprobación.";

                return RedirectToAction("Crear");
            }
            catch (Exception ex)
            {
                RegistrarError(
                    "Error registrando prematrícula",
                    ex);

                ModelState.AddModelError(
                    "",
                    "No fue posible registrar la prematrícula.");

                CargarGrados();
                return View(modelo);
            }
        }

        // ==========================================
        // RMPR-08-002
        // LISTAR SOLICITUDES
        // ==========================================

        [AutorizarAdministrador]
        [HttpGet]
        public ActionResult Listado()
        {
            var solicitudes = db.Prematriculas
                .OrderByDescending(
                    p => p.fecha_prematricula)
                .ToList()
                .Select(p => ConvertirDto(p))
                .ToList();

            return View(solicitudes);
        }

        // ==========================================
        // RMPR-08-003
        // EDITAR PREMATRÍCULA
        // ==========================================

        [AutorizarAdministrador]
        [HttpGet]
        public ActionResult Editar(int id)
        {
            var prematricula = db.Prematriculas
                .FirstOrDefault(
                    p => p.id_prematricula == id);

            if (prematricula == null)
            {
                return HttpNotFound();
            }

            if (prematricula.estado != "Pendiente")
            {
                TempData["Error"] =
                    "Solamente se pueden modificar solicitudes pendientes.";

                return RedirectToAction("Listado");
            }

            CargarGrados();

            return View(ConvertirDto(prematricula));
        }

        [AutorizarAdministrador]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(PrematriculaDto modelo)
        {
            if (modelo == null ||
                modelo.id_prematricula <= 0)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Solicitud inválida.");
            }

            try
            {
                var existente = db.Prematriculas
                    .FirstOrDefault(p =>
                        p.id_prematricula ==
                        modelo.id_prematricula);

                if (existente == null)
                {
                    return HttpNotFound();
                }

                if (existente.estado != "Pendiente")
                {
                    TempData["Error"] =
                        "Solamente se pueden modificar solicitudes pendientes.";

                    return RedirectToAction("Listado");
                }

                modelo.esNuevoIngreso =
                    existente.es_nuevo_ingreso;

                modelo.fk_id_calendario =
                    existente.fk_id_calendario;

                if (!ModelState.IsValid)
                {
                    CargarGrados();
                    return View(modelo);
                }

                if (!GradoExiste(modelo.grado))
                {
                    ModelState.AddModelError(
                        "grado",
                        "El grado seleccionado no existe.");

                    CargarGrados();
                    return View(modelo);
                }

                string identificacion =
                    modelo.identificacion.Trim();

                bool identificacionDuplicada =
                    db.Prematriculas.Any(p =>
                        p.identificacion == identificacion &&
                        p.id_prematricula !=
                            existente.id_prematricula &&
                        p.estado == "Pendiente");

                if (identificacionDuplicada)
                {
                    ModelState.AddModelError(
                        "identificacion",
                        "Ya existe otra solicitud pendiente con esta identificación.");

                    CargarGrados();
                    return View(modelo);
                }

                if (!existente.es_nuevo_ingreso &&
                    existente.identificacion != identificacion)
                {
                    ModelState.AddModelError(
                        "identificacion",
                        "No se puede cambiar la identificación de una solicitud de renovación.");

                    CargarGrados();
                    return View(modelo);
                }

                bool aprobadaMismoPeriodo =
                    db.Prematriculas.Any(p =>
                        p.identificacion == identificacion &&
                        p.id_prematricula !=
                            existente.id_prematricula &&
                        p.fk_id_calendario ==
                            existente.fk_id_calendario &&
                        p.estado == "Aprobada");

                if (aprobadaMismoPeriodo)
                {
                    ModelState.AddModelError(
                        "identificacion",
                        "Ya existe una matrícula aprobada para este período lectivo.");

                    CargarGrados();
                    return View(modelo);
                }

                existente.identificacion =
                    identificacion;
                existente.nombre =
                    modelo.nombre.Trim();
                existente.apellido1 =
                    modelo.apellido1.Trim();
                existente.apellido2 =
                    modelo.apellido2;
                existente.genero =
                    modelo.genero;
                existente.fk_id_grado =
                    modelo.grado;
                existente.telefono =
                    modelo.telefono;
                existente.fecha_nacimiento =
                    modelo.fecha_nacimiento.Value;
                existente.tiene_adecuacion =
                    modelo.tiene_adecuacion;
                existente.tiene_discapacidad =
                    modelo.tiene_discapacidad;
                existente.contactoEmergencia =
                    modelo.contactoEmergencia;
                existente.direccion =
                    modelo.direccion;

                db.SaveChanges();

                TempData["Mensaje"] =
                    "La solicitud se actualizó correctamente.";

                return RedirectToAction("Listado");
            }
            catch (Exception ex)
            {
                RegistrarError(
                    "Error editando prematrícula",
                    ex);

                ModelState.AddModelError(
                    "",
                    "No fue posible actualizar la solicitud.");

                CargarGrados();
                return View(modelo);
            }
        }

        // ==========================================
        // RMPR-08-004 / RMPR-08-006
        // APROBAR Y VERIFICAR CUPOS
        // ==========================================

        [AutorizarAdministrador]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aprobar(int id)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Identificador inválido.");
            }

            try
            {
                using (var transaccion =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    var solicitud = db.Prematriculas
                        .FirstOrDefault(
                            p => p.id_prematricula == id);

                    if (solicitud == null)
                    {
                        return HttpNotFound();
                    }

                    if (solicitud.estado != "Pendiente")
                    {
                        TempData["Error"] =
                            "Solo se pueden aprobar solicitudes pendientes.";

                        return RedirectToAction("Listado");
                    }

                    if (!solicitud.fk_id_calendario.HasValue)
                    {
                        TempData["Error"] =
                            "La solicitud no tiene un período lectivo asignado.";

                        return RedirectToAction("Listado");
                    }

                    int idCalendario =
                        solicitud.fk_id_calendario.Value;

                    var calendario = db.Calendarios
                        .FirstOrDefault(c =>
                            c.id_calendario == idCalendario);

                    if (calendario == null)
                    {
                        TempData["Error"] =
                            "El período lectivo de la solicitud no existe.";

                        return RedirectToAction("Listado");
                    }

                    if (calendario.fecha_fin.Date <
                        DateTime.Today)
                    {
                        TempData["Error"] =
                            "No se puede aprobar una matrícula para un período lectivo finalizado.";

                        return RedirectToAction("Listado");
                    }

                    var usuario = db.Usuarios
                        .FirstOrDefault(u =>
                            u.identificacion ==
                            solicitud.identificacion);

                    if (usuario == null)
                    {
                        TempData["Error"] =
                            "El estudiante necesita un usuario registrado antes de aprobar su prematrícula.";

                        return RedirectToAction("Listado");
                    }

                    bool aprobada =
                        db.Prematriculas.Any(p =>
                            p.identificacion ==
                                solicitud.identificacion &&
                            p.id_prematricula !=
                                solicitud.id_prematricula &&
                            p.fk_id_calendario ==
                                idCalendario &&
                            p.estado == "Aprobada");

                    if (aprobada)
                    {
                        TempData["Error"] =
                            "El estudiante ya tiene una matrícula aprobada para este período.";

                        return RedirectToAction("Listado");
                    }

                    var secciones = db.Secciones
                        .Where(s =>
                            s.fk_id_grado ==
                                solicitud.fk_id_grado &&
                            s.fk_id_calendario ==
                                idCalendario)
                        .ToList();

                    if (!secciones.Any())
                    {
                        TempData["Error"] =
                            "No existen secciones para el grado solicitado en el período seleccionado.";

                        return RedirectToAction("Listado");
                    }

                    var idsSeccionesPeriodo =
                        db.Secciones
                            .Where(s =>
                                s.fk_id_calendario ==
                                    idCalendario)
                            .Select(s => s.id_seccion)
                            .ToList();

                    bool yaAsignado =
                        db.SeccionesEstudiantes.Any(e =>
                            e.fk_id_usuario ==
                                usuario.id_usuario &&
                            idsSeccionesPeriodo.Contains(
                                e.fk_id_seccion));

                    if (yaAsignado)
                    {
                        TempData["Error"] =
                            "El estudiante ya tiene una matrícula asignada en este período lectivo.";

                        return RedirectToAction("Listado");
                    }

                    int? seccionDisponible = null;

                    foreach (var seccion in secciones)
                    {
                        int ocupados =
                            db.SeccionesEstudiantes.Count(e =>
                                e.fk_id_seccion ==
                                    seccion.id_seccion);

                        if (seccion.cupo > 0 &&
                            ocupados < seccion.cupo)
                        {
                            seccionDisponible =
                                seccion.id_seccion;

                            break;
                        }
                    }

                    if (!seccionDisponible.HasValue)
                    {
                        TempData["Error"] =
                            "No hay cupos disponibles para el grado solicitado.";

                        return RedirectToAction("Listado");
                    }

                    var asignacion =
                        new SeccionEstudianteEntity
                        {
                            fk_id_seccion =
                                seccionDisponible.Value,
                            fk_id_usuario =
                                usuario.id_usuario,
                            fecha_asignacion =
                                DateTime.Now
                        };

                    db.SeccionesEstudiantes.Add(asignacion);

                    bool tieneExpediente =
                        db.Expedientes.Any(e =>
                            e.fk_id_usuario ==
                                usuario.id_usuario);

                    if (!tieneExpediente)
                    {
                        var expediente =
                            new ExpedienteEntity
                            {
                                fk_id_matricula =
                                    solicitud.id_prematricula,
                                fk_id_usuario =
                                    usuario.id_usuario,
                                correo = usuario.email,
                                contactoEmergencia =
                                    solicitud.contactoEmergencia
                            };

                        db.Expedientes.Add(expediente);
                    }

                    solicitud.estado = "Aprobada";

                    db.SaveChanges();
                    transaccion.Commit();
                }

                TempData["Mensaje"] =
                    "La solicitud fue aprobada y el estudiante fue asignado a una sección.";
            }
            catch (Exception ex)
            {
                RegistrarError(
                    "Error aprobando prematrícula",
                    ex);

                TempData["Error"] =
                    "No fue posible aprobar la solicitud. Verifique la disponibilidad de cupos.";
            }

            return RedirectToAction("Listado");
        }

        // ==========================================
        // RMPR-08-004
        // RECHAZAR PREMATRÍCULA
        // ==========================================

        [AutorizarAdministrador]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Rechazar(int id)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Identificador inválido.");
            }

            try
            {
                var solicitud = db.Prematriculas
                    .FirstOrDefault(
                        p => p.id_prematricula == id);

                if (solicitud == null)
                {
                    return HttpNotFound();
                }

                if (solicitud.estado != "Pendiente")
                {
                    TempData["Error"] =
                        "Solo se pueden rechazar solicitudes pendientes.";

                    return RedirectToAction("Listado");
                }

                solicitud.estado = "Rechazada";

                db.SaveChanges();

                TempData["Mensaje"] =
                    "La solicitud fue rechazada correctamente.";
            }
            catch (Exception ex)
            {
                RegistrarError(
                    "Error rechazando prematrícula",
                    ex);

                TempData["Error"] =
                    "No fue posible rechazar la solicitud.";
            }

            return RedirectToAction("Listado");
        }

        // ==========================================
        // RMPR-08-007
        // RENOVAR MATRÍCULA - GET
        // ==========================================

        [AutorizarAdministrador]
        [HttpGet]
        public ActionResult Renovar(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound();
            }

            var usuario = db.Usuarios
                .FirstOrDefault(u =>
                    u.id_usuario == id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            bool estudianteActivo =
                db.SeccionesEstudiantes.Any(e =>
                    e.fk_id_usuario ==
                        usuario.id_usuario);

            if (!estudianteActivo)
            {
                TempData["Error"] =
                    "El estudiante no tiene una asignación registrada.";

                return RedirectToAction(
                    "EstudiantesActivos");
            }

            var anterior = db.Prematriculas
                .Where(p =>
                    p.identificacion ==
                        usuario.identificacion &&
                    p.estado == "Aprobada")
                .OrderByDescending(
                    p => p.fecha_prematricula)
                .FirstOrDefault();

            if (anterior == null)
            {
                TempData["Error"] =
                    "No existe una prematrícula aprobada anterior para este estudiante.";

                return RedirectToAction(
                    "EstudiantesActivos");
            }

            var calendarioDestino =
                ObtenerCalendarioSiguiente();

            if (calendarioDestino == null)
            {
                TempData["Error"] =
                    "No existe un calendario configurado para el próximo curso lectivo.";

                return RedirectToAction(
                    "EstudiantesActivos");
            }

            if (anterior.fk_id_calendario.HasValue)
            {
                var calendarioAnterior = db.Calendarios
                    .FirstOrDefault(c =>
                        c.id_calendario ==
                            anterior.fk_id_calendario.Value);

                if (calendarioAnterior != null &&
                    calendarioAnterior.fecha_inicio.Year >=
                        calendarioDestino.fecha_inicio.Year)
                {
                    TempData["Error"] =
                        "El estudiante ya tiene una matrícula aprobada para este año o uno posterior.";

                    return RedirectToAction(
                        "EstudiantesActivos");
                }
            }

            bool duplicada = db.Prematriculas.Any(p =>
                p.identificacion ==
                    usuario.identificacion &&
                p.fk_id_calendario ==
                    calendarioDestino.id_calendario &&
                (p.estado == "Pendiente" ||
                 p.estado == "Aprobada"));

            if (duplicada)
            {
                TempData["Error"] =
                    "El estudiante ya tiene una solicitud pendiente o aprobada para el próximo período.";

                return RedirectToAction(
                    "EstudiantesActivos");
            }

            CargarGrados();

            var modelo = ConvertirDto(anterior);

            modelo.id_prematricula =
                anterior.id_prematricula;

            modelo.esNuevoIngreso = false;
            modelo.estado = "Pendiente";

            modelo.fk_id_calendario =
                calendarioDestino.id_calendario;

            modelo.nombre_calendario =
                calendarioDestino.nombre;

            return View(modelo);
        }

        // ==========================================
        // RMPR-08-007
        // RENOVAR MATRÍCULA - POST
        // ==========================================

        [AutorizarAdministrador]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renovar(PrematriculaDto modelo)
        {
            if (modelo == null ||
                modelo.id_prematricula <= 0)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Solicitud inválida.");
            }

            try
            {
                var anterior = db.Prematriculas
                    .FirstOrDefault(p =>
                        p.id_prematricula ==
                            modelo.id_prematricula);

                if (anterior == null)
                {
                    return HttpNotFound();
                }

                if (anterior.estado != "Aprobada")
                {
                    TempData["Error"] =
                        "Solo se puede renovar a partir de una matrícula aprobada.";

                    return RedirectToAction(
                        "EstudiantesActivos");
                }

                var usuario = db.Usuarios
                    .FirstOrDefault(u =>
                        u.identificacion ==
                            anterior.identificacion);

                if (usuario == null)
                {
                    TempData["Error"] =
                        "No se encontró el usuario del estudiante.";

                    return RedirectToAction(
                        "EstudiantesActivos");
                }

                bool tieneAsignacion =
                    db.SeccionesEstudiantes.Any(e =>
                        e.fk_id_usuario ==
                            usuario.id_usuario);

                if (!tieneAsignacion)
                {
                    TempData["Error"] =
                        "El estudiante no tiene una asignación registrada.";

                    return RedirectToAction(
                        "EstudiantesActivos");
                }

                var calendarioDestino =
                    ObtenerCalendarioSiguiente();

                if (calendarioDestino == null)
                {
                    TempData["Error"] =
                        "No existe un calendario para el próximo curso lectivo.";

                    return RedirectToAction(
                        "EstudiantesActivos");
                }

                // Los datos personales se recuperan
                // desde la matrícula anterior.
                // Solo se permite seleccionar el grado.

                ModelState.Clear();

                if (!GradoExiste(modelo.grado))
                {
                    ModelState.AddModelError(
                        "grado",
                        "Seleccione un grado válido.");
                }

                if (anterior.fecha_nacimiento.Date >
                    DateTime.Today)
                {
                    ModelState.AddModelError(
                        "",
                        "La fecha de nacimiento registrada no es válida.");
                }

                if (anterior.fk_id_calendario.HasValue)
                {
                    var calendarioAnterior =
                        db.Calendarios.FirstOrDefault(c =>
                            c.id_calendario ==
                                anterior.fk_id_calendario.Value);

                    if (calendarioAnterior != null &&
                        calendarioAnterior.fecha_inicio.Year >=
                            calendarioDestino.fecha_inicio.Year)
                    {
                        ModelState.AddModelError(
                            "",
                            "La matrícula de origen no corresponde a un período anterior.");
                    }
                }

                bool solicitudPendiente =
                    db.Prematriculas.Any(p =>
                        p.identificacion ==
                            anterior.identificacion &&
                        p.fk_id_calendario ==
                            calendarioDestino.id_calendario &&
                        (p.estado == "Pendiente" ||
                         p.estado == "Aprobada"));

                if (solicitudPendiente)
                {
                    ModelState.AddModelError(
                        "",
                        "El estudiante ya tiene una solicitud pendiente o aprobada para este período lectivo.");
                }

                if (!ModelState.IsValid)
                {
                    CargarGrados();

                    var vista = ConvertirDto(anterior);

                    vista.grado = modelo.grado;
                    vista.esNuevoIngreso = false;
                    vista.fk_id_calendario =
                        calendarioDestino.id_calendario;
                    vista.nombre_calendario =
                        calendarioDestino.nombre;

                    return View(vista);
                }

                using (var transaccion =
                    db.Database.BeginTransaction(
                        IsolationLevel.Serializable))
                {
                    bool duplicada =
                        db.Prematriculas.Any(p =>
                            p.identificacion ==
                                anterior.identificacion &&
                            p.fk_id_calendario ==
                                calendarioDestino.id_calendario &&
                            (p.estado == "Pendiente" ||
                             p.estado == "Aprobada"));

                    if (duplicada)
                    {
                        ModelState.AddModelError(
                            "",
                            "Ya existe una solicitud para este período.");

                        CargarGrados();

                        var vista = ConvertirDto(anterior);

                        vista.grado = modelo.grado;
                        vista.esNuevoIngreso = false;
                        vista.fk_id_calendario =
                            calendarioDestino.id_calendario;
                        vista.nombre_calendario =
                            calendarioDestino.nombre;

                        return View(vista);
                    }

                    var nueva = new PrematriculaEntity
                    {
                        identificacion =
                            anterior.identificacion,
                        nombre = anterior.nombre,
                        apellido1 = anterior.apellido1,
                        apellido2 = anterior.apellido2,
                        genero = anterior.genero,
                        fk_id_grado = modelo.grado,
                        telefono = anterior.telefono,
                        fecha_nacimiento =
                            anterior.fecha_nacimiento,
                        tiene_adecuacion =
                            anterior.tiene_adecuacion,
                        tiene_discapacidad =
                            anterior.tiene_discapacidad,
                        contactoEmergencia =
                            anterior.contactoEmergencia,
                        direccion = anterior.direccion,
                        es_nuevo_ingreso = false,
                        fecha_prematricula = DateTime.Now,
                        estado = "Pendiente",
                        fk_id_calendario =
                            calendarioDestino.id_calendario
                    };

                    db.Prematriculas.Add(nueva);
                    db.SaveChanges();

                    transaccion.Commit();
                }

                TempData["Mensaje"] =
                    "La renovación de matrícula fue registrada correctamente.";

                return RedirectToAction("Listado");
            }
            catch (Exception ex)
            {
                RegistrarError(
                    "Error renovando matrícula",
                    ex);

                TempData["Error"] =
                    "No fue posible registrar la renovación.";

                return RedirectToAction(
                    "EstudiantesActivos");
            }
        }

        // ==========================================
        // RMPR-08-008
        // COMPROBANTE DE MATRÍCULA
        // ==========================================

        [AutorizarAdministrador]
        [HttpGet]
        public ActionResult Comprobante(int id)
        {
            var solicitud = db.Prematriculas
                .FirstOrDefault(p =>
                    p.id_prematricula == id);

            if (solicitud == null)
            {
                return HttpNotFound();
            }

            if (solicitud.estado != "Aprobada")
            {
                TempData["Error"] =
                    "El comprobante solamente está disponible para matrículas aprobadas.";

                return RedirectToAction("Listado");
            }

            var modelo = ConvertirDto(solicitud);

            // Pendiente: obtener la cuota oficial.
            // No representa un monto confirmado.
            modelo.cuota_voluntaria = 0m;

            return View(modelo);
        }

        // ==========================================
        // RMPR-08-007
        // ESTUDIANTES ACTIVOS
        // ACTUALIZADO CON ESTADO DE RENOVACIÓN
        // ==========================================

        [AutorizarAdministrador]
        [HttpGet]
        public ActionResult EstudiantesActivos()
        {
            var calendarioActual =
                ObtenerCalendarioVigente();

            var calendarioDestino =
                ObtenerCalendarioSiguiente();

            ViewBag.PeriodoDestino =
                calendarioDestino != null
                    ? calendarioDestino.nombre
                    : "No configurado";

            ViewBag.EstadosRenovacion =
                new Dictionary<string, string>();

            if (calendarioActual == null)
            {
                TempData["Error"] =
                    "No existe un período lectivo vigente.";

                return View(
                    new List<UsuarioEntity>());
            }

            // Obtener las secciones del período actual.
            var idsSecciones = db.Secciones
                .Where(s =>
                    s.fk_id_calendario ==
                        calendarioActual.id_calendario)
                .Select(s => s.id_seccion)
                .ToList();

            // Obtener estudiantes asignados
            // al período lectivo vigente.
            var estudiantes = db.Usuarios
                .Where(u =>
                    db.SeccionesEstudiantes.Any(e =>
                        e.fk_id_usuario ==
                            u.id_usuario &&
                        idsSecciones.Contains(
                            e.fk_id_seccion)))
                .OrderBy(u => u.nombre)
                .ToList();

            // Guardar el estado de renovación
            // de cada estudiante.
            var estados =
                new Dictionary<string, string>();

            if (calendarioDestino != null)
            {
                var identificaciones = estudiantes
                    .Select(e => e.identificacion)
                    .Where(i => i != null)
                    .Distinct()
                    .ToList();

                var renovaciones = db.Prematriculas
                    .Where(p =>
                        identificaciones.Contains(
                            p.identificacion) &&
                        p.fk_id_calendario ==
                            calendarioDestino.id_calendario &&
                        !p.es_nuevo_ingreso &&
                        (p.estado == "Pendiente" ||
                         p.estado == "Aprobada"))
                    .ToList();

                foreach (var estudiante in estudiantes)
                {
                    var renovacion = renovaciones
                        .Where(p =>
                            p.identificacion ==
                                estudiante.identificacion)
                        .OrderByDescending(p =>
                            p.estado == "Aprobada")
                        .ThenByDescending(p =>
                            p.fecha_prematricula)
                        .FirstOrDefault();

                    estados[estudiante.id_usuario] =
                        renovacion != null
                            ? renovacion.estado
                            : "Sin renovación";
                }
            }

            ViewBag.EstadosRenovacion = estados;

            return View(estudiantes);
        }

        // ==========================================
        // MÉTODOS AUXILIARES
        // ==========================================

        private bool EsEncargado()
        {
            var usuario = Session["UsuarioActual"]
                as UsuarioDto;

            return usuario != null &&
                string.Equals(
                    usuario.nombreRol,
                    "Encargado",
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool GradoExiste(int idGrado)
        {
            return idGrado > 0 &&
                db.Grados.Any(g =>
                    g.id_grado == idGrado);
        }

        // ==========================================
        // OBTENER CALENDARIO VIGENTE
        // ==========================================

        private CalendarioEntity ObtenerCalendarioVigente()
        {
            DateTime hoy = DateTime.Today;

            return db.Calendarios
                .ToList()
                .Where(c =>
                    c.fecha_inicio.Date <= hoy &&
                    c.fecha_fin.Date >= hoy)
                .OrderByDescending(c =>
                    c.fecha_inicio)
                .FirstOrDefault();
        }

        // ==========================================
        // OBTENER CALENDARIO DEL SIGUIENTE AÑO
        // EJEMPLO: 2026 -> 2027
        // ==========================================

        private CalendarioEntity ObtenerCalendarioSiguiente()
        {
            int siguienteAnio =
                DateTime.Today.Year + 1;

            return db.Calendarios
                .ToList()
                .Where(c =>
                    c.fecha_inicio.Year ==
                        siguienteAnio)
                .OrderBy(c =>
                    c.fecha_inicio)
                .FirstOrDefault();
        }

        // ==========================================
        // CONVERTIR ENTIDAD A DTO
        // ==========================================

        private PrematriculaDto ConvertirDto(
            PrematriculaEntity p)
        {
            var grado = db.Grados
                .FirstOrDefault(g =>
                    g.id_grado == p.fk_id_grado);

            CalendarioEntity calendario = null;

            if (p.fk_id_calendario.HasValue)
            {
                calendario = db.Calendarios
                    .FirstOrDefault(c =>
                        c.id_calendario ==
                            p.fk_id_calendario.Value);
            }

            return new PrematriculaDto
            {
                id_prematricula =
                    p.id_prematricula,

                identificacion =
                    p.identificacion,

                nombre =
                    p.nombre,

                apellido1 =
                    p.apellido1,

                apellido2 =
                    p.apellido2,

                genero =
                    p.genero,

                grado =
                    p.fk_id_grado,

                nombre_grado =
                    grado != null
                        ? grado.nombre
                        : "Sin grado",

                telefono =
                    p.telefono,

                fecha_nacimiento =
                    p.fecha_nacimiento,

                tiene_adecuacion =
                    p.tiene_adecuacion,

                tiene_discapacidad =
                    p.tiene_discapacidad,

                contactoEmergencia =
                    p.contactoEmergencia,

                direccion =
                    p.direccion,

                esNuevoIngreso =
                    p.es_nuevo_ingreso,

                estado =
                    p.estado,

                fecha_prematricula =
                    p.fecha_prematricula,

                fk_id_calendario =
                    p.fk_id_calendario,

                nombre_calendario =
                    calendario != null
                        ? calendario.nombre
                        : "Sin período asignado"
            };
        }

        // ==========================================
        // CARGAR GRADOS
        // ==========================================

        private void CargarGrados()
        {
            ViewBag.Grados = db.Grados
                .OrderBy(g => g.nivel)
                .ToList()
                .Select(g => new SelectListItem
                {
                    Value = g.id_grado.ToString(),
                    Text = g.nombre
                })
                .ToList();
        }

        // ==========================================
        // REGISTRAR ERRORES
        // ==========================================

        private void RegistrarError(
            string mensaje,
            Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                mensaje + ": " + ex);
        }

        // ==========================================
        // LIBERAR RECURSOS
        // ==========================================

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
