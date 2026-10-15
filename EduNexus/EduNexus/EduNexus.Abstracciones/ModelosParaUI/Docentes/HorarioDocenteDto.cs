using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Docentes
{
    public class DocenteBusquedaDto
    {
        public string id_usuario { get; set; }
        public string nombre { get; set; }
        public string identificacion { get; set; }
        public string email { get; set; }
        public string estado { get; set; }
    }

    public class HorarioDocenteDto
    {
        public string id_usuario { get; set; }
        public string docente { get; set; }
        public string identificacion { get; set; }
        public string email { get; set; }
        public List<HorarioDocenteItemDto> horarios { get; set; } = new List<HorarioDocenteItemDto>();
    }

    public class HorarioDocenteItemDto
    {
        public string dia_semana { get; set; }
        public string hora_inicio { get; set; }
        public string hora_fin { get; set; }
        public string materia { get; set; }
        public string seccion { get; set; }
    }
}
