using EduNexus.Abstracciones.ModelosParaUI.Expedientes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class ExpedientesController : Controller
    {
        // GET: Expedientes
        public ActionResult ListadoDeExpedientes()
        {
            List<ExpedienteDto> expedientes = ObtenerExpedientes();
            return View(expedientes);
        }

        // GET: Expedientes/Details/5
        public ActionResult DetalleDeExpediente(int? id)
        {
            List<ExpedienteDto> expedientes = ObtenerExpedientes();
            ExpedienteDto expediente = expedientes.FirstOrDefault(x => x.id_expediente == id);
            return View(expediente);
        }

        // GET: Expedientes/Create
        public ActionResult CrearExpediente()
        {
            return View();
        }

        // POST: Expedientes/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("ListadoDeExpedientes");
            }
            catch
            {
                return View();
            }
        }

        // GET: Expedientes/Edit/5
        public ActionResult EditarExpediente(int? id)
        {
            List<ExpedienteDto> expedientes = ObtenerExpedientes();
            ExpedienteDto expediente = expedientes.FirstOrDefault(x => x.id_expediente == id);
            return View(expediente);
        }

        // POST: Expedientes/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("ListadoDeExpedientes");
            }
            catch
            {
                return View();
            }
        }

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
                    direccion = "San José, San Isidro"
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
                    direccion = "Cartago, Centro"
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
                    direccion = "Heredia, San Francisco"
                },
            };
        }
    }
}