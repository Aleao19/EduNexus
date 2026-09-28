using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace EduNexus.UI.Models
{
    // Precondición de las historias: el administrador debe haber iniciado sesión.
    // Se aplica a los módulos de administración (Roles, Usuarios, Grados, Secciones).
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AutorizarAdministradorAttribute : ActionFilterAttribute
    {
        public const string ClaveSesion = "UsuarioActual";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var usuario = filterContext.HttpContext.Session[ClaveSesion] as UsuarioDto;

            if (usuario == null)
            {
                filterContext.Controller.TempData["MensajeLogin"] = "Debe iniciar sesión para acceder a este módulo.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Login" }));
                return;
            }

            if (!string.Equals(usuario.nombreRol, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Controller.TempData["MensajeError"] = "Solo un administrador puede acceder a este módulo.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
