
using EduNexus.Abstracciones.ModelosParaUI.Expedientes;
using EduNexus.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    [AutorizarAdministrador]
    public class ExpedientesController : Controller
    {
        // =========================================
        // LISTADO DE EXPEDIENTES
        // =========================================

        public ActionResult ListadoDeExpedientes()
        {
            List<ExpedienteDto> expedientes = ObtenerExpedientes();

            return View(expedientes);
        }

        // =========================================
        // DETALLE DE EXPEDIENTE
        // =========================================

        public ActionResult DetalleDeExpediente(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            ExpedienteDto expediente = ObtenerExpedientes()
                .FirstOrDefault(x => x.id_expediente == id.Value);

            if (expediente == null)
            {
                return HttpNotFound();
            }

            return View(expediente);
        }

        // =========================================
        // REXP-03-001
        // CREAR EXPEDIENTE ESTUDIANTIL
        // =========================================

        [HttpGet]
        public ActionResult CrearExpediente()
        {
            CargarPrematriculas();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearExpediente(ExpedienteDto expediente)
        {
            if (expediente == null)
            {
                return new HttpStatusCodeResult(400);
            }

            if (expediente.id_matricula <= 0)
            {
                ModelState.AddModelError(
                    "id_matricula",
                    "Debe seleccionar una matricula valida."
                );
            }

            if (!ModelState.IsValid)
            {
                CargarPrematriculas();

                return View(expediente);
            }

            // =========================================
            // PENDIENTE DE BASE DE DATOS
            // =========================================

            // 1. Buscar usuario estudiante.
            // 2. Verificar prematricula.
            // 3. Validar relaciones.
            // 4. Convertir DTO a entidad.
            // 5. Guardar en MySQL.

            ModelState.AddModelError(
                "",
                "Los datos del formulario fueron validados, " +
                "pero el expediente no se ha guardado. " +
                "Esta pendiente verificar la matricula, " +
                "el usuario estudiante y la conexion a MySQL."
            );

            CargarPrematriculas();

            return View(expediente);
        }

        // =========================================
        // CARGAR PREMATRICULAS
        // =========================================

        private void CargarPrematriculas()
        {
            // Pendiente de MySQL.
            // No se agregan matriculas ficticias.

            ViewBag.Matriculas = new SelectList(
                new List<SelectListItem>(),
                "Value",
                "Text"
            );
        }

        // =========================================
        // REXP-03-006
        // MODIFICAR EXPEDIENTE ESTUDIANTIL
        // =========================================

        // GET: Expedientes/EditarExpediente/5
        [HttpGet]
        public ActionResult EditarExpediente(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            ExpedienteDto expediente = ObtenerExpedientes()
                .FirstOrDefault(x => x.id_expediente == id.Value);

            if (expediente == null)
            {
                return HttpNotFound();
            }

            return View(expediente);
        }

        // POST: Expedientes/EditarExpediente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarExpediente(
            [Bind(Include =
                "id_expediente,nombre,apellido1,apellido2," +
                "identificacion,genero,grupo_sanguineo," +
                "alergias,correo,contactoEmergencia")]
            ExpedienteDto modelo)
        {
            if (modelo == null || modelo.id_expediente <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            // =========================================
            // VERIFICAR EXPEDIENTE DE EJEMPLO
            // =========================================

            ExpedienteDto expedienteExistente =
                ObtenerExpedientes().FirstOrDefault(
                    x => x.id_expediente == modelo.id_expediente
                );

            if (expedienteExistente == null)
            {
                return HttpNotFound();
            }

            // =========================================
            // VALIDAR SOLO LOS CAMPOS EDITABLES
            // =========================================

            // ExpedienteDto tambien se utiliza para crear.
            // En esta pantalla no se editan los demas campos.

            string[] camposEditables =
            {
                "id_expediente",
                "nombre",
                "apellido1",
                "apellido2",
                "identificacion",
                "genero",
                "grupo_sanguineo",
                "alergias",
                "correo",
                "contactoEmergencia"
            };

            foreach (string campo in ModelState.Keys.ToList())
            {
                if (!camposEditables.Contains(campo))
                {
                    ModelState.Remove(campo);
                }
            }

            // =========================================
            // VALIDAR NOMBRE
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.nombre))
            {
                ModelState.AddModelError(
                    "nombre",
                    "Debe ingresar el nombre del estudiante."
                );
            }

            // =========================================
            // VALIDAR PRIMER APELLIDO
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.apellido1))
            {
                ModelState.AddModelError(
                    "apellido1",
                    "Debe ingresar el primer apellido."
                );
            }

            // =========================================
            // VALIDAR IDENTIFICACION
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.identificacion))
            {
                ModelState.AddModelError(
                    "identificacion",
                    "Debe ingresar la cedula del estudiante."
                );
            }

            // =========================================
            // VALIDAR SEXO
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.genero))
            {
                ModelState.AddModelError(
                    "genero",
                    "Debe seleccionar el sexo."
                );
            }
            else if (modelo.genero != "Masculino" &&
                     modelo.genero != "Femenino")
            {
                ModelState.AddModelError(
                    "genero",
                    "Debe seleccionar una opcion valida."
                );
            }

            // =========================================
            // VALIDAR GRUPO SANGUINEO
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.grupo_sanguineo))
            {
                ModelState.AddModelError(
                    "grupo_sanguineo",
                    "Debe seleccionar el grupo sanguineo."
                );
            }
            else
            {
                string[] gruposPermitidos =
                {
                    "O+", "O-", "A+", "A-",
                    "B+", "B-", "AB+", "AB-"
                };

                if (!gruposPermitidos.Contains(modelo.grupo_sanguineo))
                {
                    ModelState.AddModelError(
                        "grupo_sanguineo",
                        "Debe seleccionar un grupo sanguineo valido."
                    );
                }
            }

            // =========================================
            // VALIDAR ALERGIAS
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.alergias))
            {
                ModelState.AddModelError(
                    "alergias",
                    "Debe indicar las alergias o escribir Ninguna."
                );
            }

            // =========================================
            // VALIDAR CORREO
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.correo))
            {
                ModelState.AddModelError(
                    "correo",
                    "Debe ingresar el correo electronico."
                );
            }

            // El atributo EmailAddress del DTO
            // valida el formato del correo.

            // =========================================
            // VALIDAR CONTACTO DE EMERGENCIA
            // =========================================

            if (string.IsNullOrWhiteSpace(modelo.contactoEmergencia))
            {
                ModelState.AddModelError(
                    "contactoEmergencia",
                    "Debe ingresar el contacto de emergencia."
                );
            }

            // =========================================
            // COMPROBAR VALIDACIONES
            // =========================================

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // =========================================
            // PENDIENTE DE MYSQL
            // =========================================

            // Cuando exista acceso a la base de datos:
            //
            // 1. Buscar el expediente real por ID.
            //
            // 2. Actualizar exclusivamente:
            //    nombre
            //    apellido1
            //    apellido2
            //    identificacion
            //    genero
            //    grupo_sanguineo
            //    alergias
            //    correo
            //    contactoEmergencia
            //
            // 3. Conservar los demas datos.
            //
            // 4. Guardar cambios en MySQL.
            //
            // Actualmente NO se modifica el expediente.

            ModelState.AddModelError(
                "",
                "Los datos del expediente fueron validados, " +
                "pero los cambios no se han guardado. " +
                "Esta pendiente la conexion con MySQL."
            );

            return View(modelo);
        }

        // =========================================
        // EXPEDIENTES DE EJEMPLO
        // =========================================

        private List<ExpedienteDto> ObtenerExpedientes()
        {
            return new List<ExpedienteDto>
            {
                new ExpedienteDto
                {
                    id_expediente = 1,
                    id_matricula = 101,
                    identificacion = "1-1234-5678",
                    nombre = "Sofía",
                    apellido1 = "Castro",
                    apellido2 = "Mora",
                    genero = "Femenino",
                    grado = 1,
                    telefono = "8888-0001",
                    fecha_nacimiento = new DateTime(2019, 3, 12),
                    tiene_adecuacion = false,
                    tiene_discapacidad = false,
                    contactoEmergencia = "Marta Mora - 8888-1001",
                    direccion = "San José, San Isidro",
                    correo = "sofia@example.com",
                    grupo_sanguineo = "O+",
                    alergias = "Ninguna"
                },
                new ExpedienteDto
                {
                    id_expediente = 2,
                    id_matricula = 102,
                    identificacion = "2-2345-6789",
                    nombre = "Diego",
                    apellido1 = "Fernández",
                    apellido2 = "Rojas",
                    genero = "Masculino",
                    grado = 2,
                    telefono = "8888-0002",
                    fecha_nacimiento = new DateTime(2018, 7, 25),
                    tiene_adecuacion = true,
                    tiene_discapacidad = false,
                    contactoEmergencia = "Pedro Fernández - 8888-1002",
                    direccion = "Cartago, Centro",
                    correo = "diego@example.com",
                    grupo_sanguineo = "A+",
                    alergias = "Ninguna"
                },
                new ExpedienteDto
                {
                    id_expediente = 3,
                    id_matricula = 103,
                    identificacion = "3-3456-7890",
                    nombre = "Valeria",
                    apellido1 = "Gómez",
                    apellido2 = "Vindas",
                    genero = "Femenino",
                    grado = 3,
                    telefono = "8888-0003",
                    fecha_nacimiento = new DateTime(2017, 11, 5),
                    tiene_adecuacion = false,
                    tiene_discapacidad = true,
                    contactoEmergencia = "Laura Vindas - 8888-1003",
                    direccion = "Heredia, San Francisco",
                    correo = "valeria@example.com",
                    grupo_sanguineo = "B+",
                    alergias = "Ninguna"
                }
            };
        }
    }
}
