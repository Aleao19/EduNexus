using EduNexus.Abstracciones.ModelosParaUI.Roles;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class UsuariosController : Controller
    {
        // Dominio institucional del MEP exigido para roles administrativos/docentes (criterio RSEG-01-002 #5).
        private const string DominioInstitucional = "@mep.go.cr";

        // Roles que, por su nivel de acceso, deben registrarse con correo institucional del MEP.
        private static readonly string[] RolesQueRequierenCorreoInstitucional = { "Administrador", "Director", "Docente" };

        // Almacén en memoria compartido (ver nota en RolesController). Se usa también desde
        // HomeController para validar credenciales e impedir el acceso a usuarios bloqueados.
        public static List<UsuarioDto> Usuarios = new List<UsuarioDto>
        {
            new UsuarioDto
            {
                id_usuario = 1,
                cedula = "1-1111-1111",
                nombre = "Pedro",
                apellido1 = "Araya",
                apellido2 = "Mora",
                correo = "pedro.araya@mep.go.cr",
                id_rol = 1,
                nombreRol = "Administrador",
                estado = "Activo",
                contrasenna = "Admin123"
            }
        };

        // GET: Usuarios
        public ActionResult ListadoDeUsuarios()
        {
            return View(Usuarios);
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
            ValidarUsuarioNuevo(usuario);

            if (!ModelState.IsValid)
            {
                CargarRolesEnViewBag();
                return View(usuario);
            }

            var rol = RolesController.Roles.FirstOrDefault(r => r.id_rol == usuario.id_rol);

            usuario.id_usuario = Usuarios.Any() ? Usuarios.Max(x => x.id_usuario) + 1 : 1;
            usuario.cedula = usuario.cedula.Trim();
            usuario.correo = usuario.correo.Trim();
            usuario.nombreRol = rol?.nombre;
            if (string.IsNullOrWhiteSpace(usuario.estado))
            {
                usuario.estado = "Activo";
            }

            Usuarios.Add(usuario);

            TempData["MensajeExito"] = $"El usuario \"{usuario.nombre} {usuario.apellido1}\" se registró correctamente.";
            return RedirectToAction("ListadoDeUsuarios");
        }

        // POST: Usuarios/BloquearUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BloquearUsuario(int id)
        {
            var usuario = Usuarios.FirstOrDefault(u => u.id_usuario == id);
            if (usuario != null)
            {
                usuario.estado = "Inactivo";
                TempData["MensajeExito"] = $"El usuario \"{usuario.nombre} {usuario.apellido1}\" fue bloqueado/desactivado.";
            }

            return RedirectToAction("ListadoDeUsuarios");
        }

        // POST: Usuarios/ActivarUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActivarUsuario(int id)
        {
            var usuario = Usuarios.FirstOrDefault(u => u.id_usuario == id);
            if (usuario != null)
            {
                usuario.estado = "Activo";
                TempData["MensajeExito"] = $"El usuario \"{usuario.nombre} {usuario.apellido1}\" fue reactivado.";
            }

            return RedirectToAction("ListadoDeUsuarios");
        }

        private void ValidarUsuarioNuevo(UsuarioDto usuario)
        {
            // Criterio: "Registro con correo ya existente"
            if (!string.IsNullOrWhiteSpace(usuario?.correo) &&
                Usuarios.Any(x => x.correo.Trim().Equals(usuario.correo.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("correo", "Ya existe un usuario registrado con ese correo electrónico.");
            }

            // Criterio: "Registro con cédula ya existente"
            if (!string.IsNullOrWhiteSpace(usuario?.cedula) &&
                Usuarios.Any(x => x.cedula.Trim().Equals(usuario.cedula.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("cedula", "Ya existe un usuario registrado con esa cédula.");
            }

            // Criterio: "Registro con correo no institucional del MEP" — solo aplica a roles
            // administrativos/docentes; Padre y Estudiante pueden usar cualquier correo válido.
            if (usuario != null && !string.IsNullOrWhiteSpace(usuario.correo))
            {
                var rol = RolesController.Roles.FirstOrDefault(r => r.id_rol == usuario.id_rol);
                if (rol != null &&
                    RolesQueRequierenCorreoInstitucional.Contains(rol.nombre, StringComparer.OrdinalIgnoreCase) &&
                    !usuario.correo.Trim().EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("correo", $"El rol \"{rol.nombre}\" requiere un correo institucional del MEP ({DominioInstitucional}).");
                }
            }
        }

        private void CargarRolesEnViewBag()
        {
            ViewBag.Roles = new SelectList(RolesController.Roles, "id_rol", "nombre");
        }
    }
}
