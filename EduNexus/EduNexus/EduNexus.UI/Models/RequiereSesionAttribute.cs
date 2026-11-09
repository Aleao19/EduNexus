using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models.Identity;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace EduNexus.UI.Models
{
    // Filtro global: ninguna pantalla se muestra sin haber iniciado sesión.
    // Las acciones marcadas con [AllowAnonymous] (Login, CerrarSesion) quedan libres.
    // En cada solicitud con sesión se verifica en la BD que el usuario siga existiendo y activo:
    // si el administrador lo bloqueó o desactivó, la sesión se cierra de inmediato.
    public class RequiereSesionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool esAnonima =
                filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true);

            if (esAnonima || filterContext.IsChildAction)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            var sesion = filterContext.HttpContext.Session;
            var usuario = sesion != null ? sesion[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto : null;

            if (usuario != null)
            {
                if (UsuarioSigueActivo(usuario))
                {
                    base.OnActionExecuting(filterContext);
                    return;
                }

                // Usuario bloqueado, desactivado o eliminado mientras tenía la sesión abierta.
                sesion.Abandon();
                filterContext.Controller.TempData["MensajeLogin"] = "Su usuario fue bloqueado o desactivado.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Login" }));
                return;
            }

            string controlador = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            string accion = filterContext.ActionDescriptor.ActionName;
            bool esInicio = string.Equals(controlador, "Home", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(accion, "Index", StringComparison.OrdinalIgnoreCase);

            // Al abrir el sistema se va directo al login; si intentó entrar a otra pantalla se le avisa.
            if (!esInicio)
            {
                filterContext.Controller.TempData["MensajeLogin"] = "Debe iniciar sesión para acceder a este módulo.";
            }

            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Login" }));
        }

        // Consulta la BD: el usuario debe existir y tener estado = true.
        // También actualiza el nombre del rol en la sesión por si el administrador lo cambió.
        private static bool UsuarioSigueActivo(UsuarioDto usuario)
        {
            using (var db = new EduNexusDbContext())
            {
                string id = usuario.id_usuario;
                var registro = db.Usuarios
                    .Where(u => u.id_usuario == id)
                    .Select(u => new { u.estado, u.rol })
                    .FirstOrDefault();

                if (registro == null || !registro.estado)
                {
                    return false;
                }

                if (registro.rol != usuario.id_rol)
                {
                    string nombreRol = db.Roles.Where(r => r.id_rol == registro.rol).Select(r => r.nombre).FirstOrDefault();
                    if (nombreRol == null)
                    {
                        return false;
                    }
                    usuario.id_rol = registro.rol;
                    usuario.nombreRol = nombreRol;
                }

                return true;
            }
        }
    }
}
