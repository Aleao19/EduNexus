using EduNexus.Abstracciones.ModelosParaUI.Roles;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RSEG-01-001 Como administrador quiero crear roles (Administrador, Director, Docente, Padre, Estudiante)
    // para controlar los permisos de acceso.
    [AutorizarAdministrador]
    public class RolesController : Controller
    {
        // GET: Roles
        public ActionResult ListadoDeRoles()
        {
            using (var db = new EduNexusDbContext())
            {
                var roles = db.Roles
                    .OrderBy(r => r.nombre)
                    .Select(r => new RolDto
                    {
                        id_rol = r.id_rol,
                        nombre = r.nombre,
                        cantidadUsuarios = db.Usuarios.Count(u => u.rol == r.id_rol)
                    })
                    .ToList();

                return View(roles);
            }
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
            using (var db = new EduNexusDbContext())
            {
                // Escenario 2: rol con nombre duplicado
                if (!string.IsNullOrWhiteSpace(rol?.nombre))
                {
                    string nombre = rol.nombre.Trim();
                    bool existe = db.Roles.Select(r => r.nombre).ToList()
                        .Any(n => n.Trim().Equals(nombre, StringComparison.OrdinalIgnoreCase));
                    if (existe)
                    {
                        ModelState.AddModelError("nombre", "El nombre del rol ya está en uso.");
                    }
                }

                // Escenario 3: creación con campos incompletos
                if (!ModelState.IsValid)
                {
                    return View(rol);
                }

                // Escenario 1: creación exitosa
                db.Roles.Add(new RolEntity
                {
                    id_rol = Guid.NewGuid().ToString(),
                    nombre = rol.nombre.Trim()
                });
                db.SaveChanges();
            }

            TempData["MensajeExito"] = "El rol \"" + rol.nombre.Trim() + "\" se creó correctamente.";
            return RedirectToAction("ListadoDeRoles");
        }
    }
}
