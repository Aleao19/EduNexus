
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace EduNexus.UI.Models
{
    // Permite el acceso únicamente a usuarios
    // autenticados con el rol Administrador.
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public class AutorizarAdministradorAttribute : ActionFilterAttribute
    {
        public const string ClaveSesion = "UsuarioActual";

        public override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            // 1. Obtener la sesión actual.
            var sesion = filterContext.HttpContext.Session;

            if (sesion == null)
            {
                RedirigirAlLogin(filterContext);
                return;
            }

            // 2. Obtener el usuario autenticado.
            var usuario = sesion[ClaveSesion] as UsuarioDto;

            // 3. Validar que haya iniciado sesión.
            if (usuario == null)
            {
                RedirigirAlLogin(filterContext);
                return;
            }

            // 4. Validar que tenga el rol Administrador
            //    (o Coordinador Academico en las acciones marcadas con [PermitirCoordinador]).
            //    Se compara con RolesSistema (sin importar tildes ni mayúsculas).
            bool esAdministrador = RolesSistema.Es(usuario, RolesSistema.Administrador);
            bool esCoordinador = RolesSistema.Es(usuario, RolesSistema.CoordinadorAcademico);

            bool accionPermiteCoordinador = filterContext.ActionDescriptor
                .IsDefined(typeof(PermitirCoordinadorAttribute), true);

            if (!esAdministrador && !(esCoordinador && accionPermiteCoordinador))
            {
                filterContext.Controller.TempData["MensajeError"] =
                    "No tiene permisos para acceder a este módulo. " +
                    "Esta funcionalidad es exclusiva del administrador.";

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new
                        {
                            controller = "Home",
                            action = "Index"
                        }
                    )
                );

                return;
            }

            // 5. Permitir continuar con la acción solicitada.
            base.OnActionExecuting(filterContext);
        }

        // Redirigir cuando no existe una sesión válida.
        private void RedirigirAlLogin(
            ActionExecutingContext filterContext)
        {
            filterContext.Controller.TempData["MensajeLogin"] =
                "Debe iniciar sesión para acceder a este módulo.";

            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(
                    new
                    {
                        controller = "Home",
                        action = "Login"
                    }
                )
            );
        }
    }
}
