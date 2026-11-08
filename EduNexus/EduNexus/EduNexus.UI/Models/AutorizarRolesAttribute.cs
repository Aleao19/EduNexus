using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace EduNexus.UI.Models
{
    // Permite el acceso a usuarios autenticados con alguno de los roles indicados.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AutorizarRolesAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public AutorizarRolesAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var usuario = filterContext.HttpContext.Session?[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto;

            if (usuario == null)
            {
                filterContext.Controller.TempData["MensajeLogin"] = "Debe iniciar sesión para acceder a este módulo.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Home", action = "Login" }));
                return;
            }

            bool permitido = _roles.Any(r =>
                string.Equals(usuario.nombreRol, r, StringComparison.OrdinalIgnoreCase));

            if (!permitido)
            {
                filterContext.Controller.TempData["MensajeError"] =
                    "No tiene permisos para acceder a este módulo.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}