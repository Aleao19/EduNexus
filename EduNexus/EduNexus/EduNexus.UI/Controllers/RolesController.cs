using EduNexus.Abstracciones.ModelosParaUI.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class RolesController : Controller
    {
        // Almacén en memoria compartido mientras no exista una base de datos real.
        // Se declara static para que los datos persistan entre peticiones durante
        // la vida de la aplicación, y public para que UsuariosController pueda
        // consultar y validar contra el mismo catálogo de roles.
        public static List<RolDto> Roles = new List<RolDto>
        {
            new RolDto { id_rol = 1, nombre = "Administrador" },
            new RolDto { id_rol = 2, nombre = "Director" },
            new RolDto { id_rol = 3, nombre = "Docente" },
            new RolDto { id_rol = 4, nombre = "Padre" },
            new RolDto { id_rol = 5, nombre = "Estudiante" },
        };

        // GET: Roles
        public ActionResult ListadoDeRoles()
        {
            // Se calcula cuántos usuarios tiene asignado cada rol para mostrarlo en el listado.
            foreach (var rol in Roles)
            {
                rol.cantidadUsuarios = UsuariosController.Usuarios.Count(u => u.id_rol == rol.id_rol);
            }

            return View(Roles);
        }

        // GET: Roles/CrearRol
        [HttpGet]
        public ActionResult CrearRol()
        {
            return View(new RolDto());
        }

        // POST: Roles/CrearRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearRol(RolDto rol)
        {
            // Criterio: "Rol con nombre duplicado" — comparación sin distinguir mayúsculas/minúsculas
            // ni espacios accidentales al inicio/fin.
            if (!string.IsNullOrWhiteSpace(rol?.nombre) &&
                Roles.Any(x => x.nombre.Trim().Equals(rol.nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("nombre", "Ya existe un rol registrado con ese nombre.");
            }

            // Criterio: "Creación con campos incompletos" — lo cubren las anotaciones [Required] del DTO,
            // validadas automáticamente por ModelState al hacer binding del formulario.
            if (!ModelState.IsValid)
            {
                return View(rol);
            }

            rol.nombre = rol.nombre.Trim();
            rol.id_rol = Roles.Any() ? Roles.Max(x => x.id_rol) + 1 : 1;
            Roles.Add(rol);

            TempData["MensajeExito"] = $"El rol \"{rol.nombre}\" se creó correctamente.";
            return RedirectToAction("ListadoDeRoles");
        }
    }
}
