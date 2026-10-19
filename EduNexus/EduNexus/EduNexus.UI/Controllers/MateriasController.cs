using EduNexus.Abstracciones.ModelosParaUI.Materias;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class MateriasController : Controller
    {
        // GET: Materias
        public ActionResult ListadoDeMaterias()
        {
            List<MateriasDto> materias = ObtenerMaterias();
            return View(materias);
        }

        // GET: Materias/CrearMateria
        [HttpGet]
        public ActionResult CrearMateria()
        {
            CargarProfesores(null);
            return View(new MateriasDto());
        }

        // POST: Materias/CrearMateria
        // Se llama igual que el GET porque el formulario (Html.BeginForm()) envía a la misma URL.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearMateria(MateriasDto materiaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    CargarProfesores(materiaDto.id_profesor);
                    return View(materiaDto);
                }

                using (var db = new EduNexusDbContext())
                {
                    string nombre = materiaDto.nombre.Trim();

                    // Validar que la materia no exista
                    bool existe = db.Materias
                        .Any(m => m.nombre.ToLower() == nombre.ToLower());

                    if (existe)
                    {
                        ModelState.AddModelError("nombre", "Una materia con este nombre ya existe.");
                        CargarProfesores(materiaDto.id_profesor);
                        return View(materiaDto);
                    }

                    var nuevaMateria = new MateriaEntity
                    {
                        nombre = nombre,
                        profesor = string.IsNullOrEmpty(materiaDto.id_profesor) ? null : materiaDto.id_profesor,
                        descripcion = (materiaDto.descripcion ?? string.Empty).Trim()
                    };

                    db.Materias.Add(nuevaMateria);
                    db.SaveChanges();
                }

                return RedirectToAction("ListadoDeMaterias");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear materia: " + ex.Message);
                CargarProfesores(materiaDto.id_profesor);
                return View(materiaDto);
            }
        }

        // GET: Materias/EditarMateria/5
        [HttpGet]
        public ActionResult EditarMateria(int? id)
        {
            MateriasDto materia = ObtenerMaterias().FirstOrDefault(x => x.id_materia == id);

            if (materia == null)
            {
                return HttpNotFound();
            }

            CargarProfesores(materia.id_profesor);
            return View(materia);
        }

        // POST: Materias/EditarMateria/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarMateria(MateriasDto materiaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    CargarProfesores(materiaDto.id_profesor);
                    return View(materiaDto);
                }

                using (var db = new EduNexusDbContext())
                {
                    var materia = db.Materias.FirstOrDefault(m => m.id_materia == materiaDto.id_materia);

                    if (materia == null)
                    {
                        return HttpNotFound();
                    }

                    string nombre = materiaDto.nombre.Trim();

                    // Validar nombre duplicado en otra materia
                    bool duplicada = db.Materias
                        .Any(m => m.id_materia != materiaDto.id_materia
                               && m.nombre.ToLower() == nombre.ToLower());

                    if (duplicada)
                    {
                        ModelState.AddModelError("nombre", "Una materia con este nombre ya existe.");
                        CargarProfesores(materiaDto.id_profesor);
                        return View(materiaDto);
                    }

                    materia.nombre = nombre;
                    materia.profesor = string.IsNullOrEmpty(materiaDto.id_profesor) ? null : materiaDto.id_profesor;
                    materia.descripcion = (materiaDto.descripcion ?? string.Empty).Trim();

                    db.SaveChanges();
                }

                return RedirectToAction("ListadoDeMaterias");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar materia: " + ex.Message);
                CargarProfesores(materiaDto.id_profesor);
                return View(materiaDto);
            }
        }

        // Carga la lista de docentes para el DropDownList (ViewBag.Profesores).
        private void CargarProfesores(string idSeleccionado)
        {
            using (var db = new EduNexusDbContext())
            {
                string idRolDocente = db.Roles
                    .Where(r => r.nombre == "Docente")
                    .Select(r => r.id_rol)
                    .FirstOrDefault();

                List<UsuarioEntity> docentes = new List<UsuarioEntity>();
                if (idRolDocente != null)
                {
                    docentes = db.Usuarios
                        .Where(u => u.rol == idRolDocente)
                        .OrderBy(u => u.nombre)
                        .ThenBy(u => u.apellido1)
                        .ToList();
                }

                ViewBag.Profesores = new SelectList(
                    docentes.Select(d => new
                    {
                        Value = d.id_usuario,
                        Text = d.nombre + " " + d.apellido1 + (string.IsNullOrEmpty(d.apellido2) ? "" : " " + d.apellido2)
                    }).ToList(),
                    "Value",
                    "Text",
                    idSeleccionado
                );
            }
        }

        private List<MateriasDto> ObtenerMaterias()
        {
            using (var db = new EduNexusDbContext())
            {
                var usuarios = db.Usuarios.ToList().ToDictionary(u => u.id_usuario);

                return db.Materias
                    .ToList()
                    .Select(m => new MateriasDto
                    {
                        id_materia = m.id_materia,
                        nombre = m.nombre,
                        id_profesor = m.profesor,
                        descripcion = m.descripcion,
                        profesor = (m.profesor != null && usuarios.ContainsKey(m.profesor))
                                    ? usuarios[m.profesor].nombre + " " + usuarios[m.profesor].apellido1
                                    : string.Empty
                    })
                    .ToList();
            }
        }
    }
}