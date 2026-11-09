using EduNexus.Abstracciones.ModelosParaUI.Roles;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RSEG-01-001 Roles del sistema y sus permisos de acceso.
    // Por decisión del cliente no se crean roles nuevos desde la aplicación: solo se usan los roles
    // precargados (Administrador, Coordinador Academico, Director, Docente, Estudiante, Padre, Encargado),
    // cuyos permisos son fijos y están definidos en RolesSistema. Esta pantalla solo los consulta.
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
    }
}
