using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace EduNexus.UI.Models
{
    // Filtro global: ninguna pantalla se muestra sin haber iniciado sesión.
    // Las acciones marcadas con [AllowAnonymous] (Login, CerrarSesion, ProbarConexion) quedan libres.
    public class RequiereSesionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool esAnonima =
                filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true);

            if (esAnonima || filterContext.HttpContext.Session[AutorizarAdministradorAttribute.ClaveSesion] != null)
            {
                base.OnActionExecuting(filterContext);
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
    }
}
