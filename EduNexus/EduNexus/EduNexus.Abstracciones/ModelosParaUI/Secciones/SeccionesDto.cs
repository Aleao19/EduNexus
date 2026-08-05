using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Secciones
{
    public class SeccionesDto
    {
        public  int id_seccion { get; set; }
        public string nombre { get; set; }
        public int grado { get; set; }
        public int cupo { get; set; }
    }
}
