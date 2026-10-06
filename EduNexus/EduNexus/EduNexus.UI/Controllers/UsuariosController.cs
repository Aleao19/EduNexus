using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    // RSEG-01-002 Registrar usuarios con cédula, nombre, apellidos, estado, correo y rol.
    // RSEG-01-003 Bloquear o desactivar usuarios.
    [AutorizarAdministrador]
    public class UsuariosController : Controller
    {
        // Dominio institucional del MEP exigido para roles administrativos/docentes (RSEG-01-002 #5).
        private const string DominioInstitucional = "@mep.go.cr";

        private static readonly string[] RolesQueRequierenCorreoInstitucional = { "Administrador", "Director", "Docente" };

        // GET: Usuarios
        public ActionResult ListadoDeUsuarios()
        {
            using (var db = new EduNexusDbContext())
            {
                var usuarios = (from u in db.Usuarios
                                join r in db.Roles on u.rol equals r.id_rol into rolesUsuario
                                from r in rolesUsuario.DefaultIfEmpty()
                                orderby u.nombre, u.apellido1
                                select new { u, nombreRol = r != null ? r.nombre : "" })
                               .ToList()
                               .Select(x => AUsuarioDto(x.u, x.nombreRol))
                               .ToList();

                return View(usuarios);
            }
        }

        // GET: Usuarios/CrearUsuario
        [HttpGet]
        public ActionResult CrearUsuario()
        {
            CargarRolesEnViewBag();
            return View(new UsuarioDto { estado = "Activo" });
        }

        // POST: Usuarios/CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuario(UsuarioDto usuario)
        {
            using (var db = new EduNexusDbContext())
            {
                ValidarUsuarioNuevo(db, usuario);

                // Escenario 4: registro con datos obligatorios incompletos
                if (!ModelState.IsValid)
                {
                    CargarRolesEnViewBag();
                    return View(usuario);
                }

                // Escenario 1: registro exitoso del usuario
                db.Usuarios.Add(new UsuarioEntity
                {
                    id_usuario = Guid.NewGuid().ToString(),
                    identificacion = usuario.cedula.Trim(),
                    nombre = usuario.nombre.Trim(),
                    apellido1 = usuario.apellido1.Trim(),
                    apellido2 = string.IsNullOrWhiteSpace(usuario.apellido2) ? null : usuario.apellido2.Trim(),
                    email = usuario.correo.Trim(),
                    rol = usuario.id_rol,
                    estado = usuario.estado == "Activo",
                    password_hash = ContrasennaHasher.Cifrar(usuario.contrasenna),
                    security_stamp = Guid.NewGuid().ToString(),
                    email_confirmado = false
                });
                db.SaveChanges();
            }

            TempData["MensajeExito"] = "El usuario \"" + usuario.nombre + " " + usuario.apellido1 + "\" se registró correctamente.";
            return RedirectToAction("ListadoDeUsuarios");
        }

        // POST: Usuarios/BloquearUsuario/{id}
        // RSEG-01-003 Escenario 1: bloqueo exitoso de usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BloquearUsuario(string id)
        {
            var actual = Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;
            if (actual != null && actual.id_usuario == id)
            {
                TempData["MensajeError"] = "No puede bloquear su propio usuario mientras tiene la sesión iniciada.";
                return RedirectToAction("ListadoDeUsuarios");
            }

            CambiarEstado(id, false, "fue bloqueado/desactivado");
            return RedirectToAction("ListadoDeUsuarios");
        }

        // POST: Usuarios/ActivarUsuario/{id}
        // RSEG-01-003 Escenario 3: reactivación de usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActivarUsuario(string id)
        {
            CambiarEstado(id, true, "fue reactivado");
            return RedirectToAction("ListadoDeUsuarios");
        }

        private void CambiarEstado(string id, bool activo, string accion)
        {
            using (var db = new EduNexusDbContext())
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.id_usuario == id);
                if (usuario == null)
                {
                    TempData["MensajeError"] = "El usuario no existe.";
                    return;
                }

                usuario.estado = activo;
                db.SaveChanges();
                TempData["MensajeExito"] = "El usuario \"" + usuario.nombre + " " + usuario.apellido1 + "\" " + accion + ".";
            }
        }

        private void ValidarUsuarioNuevo(EduNexusDbContext db, UsuarioDto usuario)
        {
            if (usuario == null)
            {
                return;
            }

            // Escenario 2: registro con correo ya existente
            if (!string.IsNullOrWhiteSpace(usuario.correo))
            {
                string correo = usuario.correo.Trim();
                bool existe = db.Usuarios.Select(u => u.email).ToList()
                    .Any(c => c != null && c.Trim().Equals(correo, StringComparison.OrdinalIgnoreCase));
                if (existe)
                {
                    ModelState.AddModelError("correo", "El correo electrónico ya está en uso.");
                }
            }

            // Escenario 3: registro con cédula ya existente
            if (!string.IsNullOrWhiteSpace(usuario.cedula))
            {
                string cedula = usuario.cedula.Trim();
                bool existe = db.Usuarios.Select(u => u.identificacion).ToList()
                    .Any(c => c != null && c.Trim().Equals(cedula, StringComparison.OrdinalIgnoreCase));
                if (existe)
                {
                    ModelState.AddModelError("cedula", "La cédula ya está asociada a otro usuario.");
                }
            }

            // El rol seleccionado debe existir
            RolEntity rol = null;
            if (!string.IsNullOrWhiteSpace(usuario.id_rol))
            {
                rol = db.Roles.FirstOrDefault(r => r.id_rol == usuario.id_rol);
                if (rol == null)
                {
                    ModelState.AddModelError("id_rol", "Debe seleccionar un rol válido.");
                }
            }

            // Escenario 5: correo no institucional del MEP para Administrador, Director o Docente
            if (rol != null && !string.IsNullOrWhiteSpace(usuario.correo) &&
                RolesQueRequierenCorreoInstitucional.Contains(rol.nombre, StringComparer.OrdinalIgnoreCase) &&
                !usuario.correo.Trim().EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("correo", "Debe usarse un correo institucional del MEP (" + DominioInstitucional + ") para el rol " + rol.nombre + ".");
            }
        }

        private void CargarRolesEnViewBag()
        {
            using (var db = new EduNexusDbContext())
            {
                ViewBag.Roles = new SelectList(db.Roles.OrderBy(r => r.nombre).ToList(), "id_rol", "nombre");
            }
        }

        internal static UsuarioDto AUsuarioDto(UsuarioEntity u, string nombreRol)
        {
            return new UsuarioDto
            {
                id_usuario = u.id_usuario,
                cedula = u.identificacion,
                nombre = u.nombre,
                apellido1 = u.apellido1,
                apellido2 = u.apellido2,
                correo = u.email,
                id_rol = u.rol,
                nombreRol = nombreRol,
                estado = u.estado ? "Activo" : "Inactivo"
            };
        }
    }
}
