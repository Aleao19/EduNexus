using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace EduNexus.UI
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Nuevas rutas para recuperación de contraseña
            routes.MapRoute(
                name: "ForgotPassword",
                url: "Account/ForgotPassword",
                defaults: new { controller = "Account", action = "ForgotPassword" }
            );

            routes.MapRoute(
                name: "ResetPassword",
                url: "Account/ResetPassword",
                defaults: new { controller = "Account", action = "ResetPassword" }
            );

            // Ruta por defecto
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
