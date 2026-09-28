using System.Web;
using System.Web.Mvc;

namespace EduNexus.UI
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Todas las pantallas requieren sesión, excepto las marcadas con [AllowAnonymous]
            filters.Add(new EduNexus.UI.Models.RequiereSesionAttribute());
        }
    }
}
