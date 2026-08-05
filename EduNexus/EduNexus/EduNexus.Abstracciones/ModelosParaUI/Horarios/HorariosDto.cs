using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Horarios
{
    public class HorariosDto
    {
        public int id_horario { get; set; }
        public DateTime hora_inicio { get; set; }
        public DateTime hora_fin { get; set; }
        public string dia_semana { get; set; }
        public int seccion { get; set; }
        public int materia { get; set; }
    }
}
