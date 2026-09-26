using EduNexus.Abstracciones.ModelosParaUI.Grados;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class GradosController : Controller
    {
        public ActionResult ListadoDeGrados()
        {
            List<GradosDto> grados = ObtenerGrados();
            return View(grados);
        }

        public ActionResult DetalleDeGrado(int? id)
        {
            List<GradosDto> grados = ObtenerGrados();
            GradosDto grado = grados.FirstOrDefault(x => x.id_grado == id);

            return View(grado);
        }

        public ActionResult CrearGrado()
        {
            return View();
        }

        public ActionResult EditarGrado(int? id)
        {
            List<GradosDto> grados = ObtenerGrados();

            GradosDto grado =
                grados.FirstOrDefault(x => x.id_grado == id);

            return View(grado);
        }

        private List<GradosDto> ObtenerGrados()
        {
            return new List<GradosDto>
            {
                new GradosDto
                {
                    id_grado = 1,
                    grado = 1,
                    descripcion = "Primer Grado"
                },
                new GradosDto
                {
                    id_grado = 2,
                    grado = 2,
                    descripcion = "Segundo Grado"
                },
                new GradosDto
                {
                    id_grado = 3,
                    grado = 3,
                    descripcion = "Tercer Grado"
                },
                new GradosDto
                {
                    id_grado = 4,
                    grado = 4,
                    descripcion = "Cuarto Grado"
                },
                new GradosDto
                {
                    id_grado = 5,
                    grado = 5,
                    descripcion = "Quinto Grado"
                },
                new GradosDto
                {
                    id_grado = 6,
                    grado = 6,
                    descripcion = "Sexto Grado"
                }
            };
        }
    }
}