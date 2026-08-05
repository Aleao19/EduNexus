using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Asignaciones
{
    public class AsignacionesDto
    {
        public int id_asignacion { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public float porcentaje_nota { get; set; }
        public float calificacion { get; set; }
        public string estado { get; set; }

    }
}
