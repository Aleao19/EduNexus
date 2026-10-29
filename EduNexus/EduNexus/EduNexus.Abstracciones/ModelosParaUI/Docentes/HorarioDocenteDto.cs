using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Docentes
{
    public class DocenteBusquedaDto
    {
        public string IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Identificacion { get; set; }
        public string Email { get; set; }
        public string Estado { get; set; }
    }

    public class HorarioDocenteDto
    {
        public string IdUsuario { get; set; }
        public string Docente { get; set; }
        public string Identificacion { get; set; }
        public string Email { get; set; }
        public List<HorarioDocenteItemDto> Horarios { get; set; } = new List<HorarioDocenteItemDto>();
    }

    public class HorarioDocenteItemDto
    {
        public string DiaSemana { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }
        public string Materia { get; set; }
        public string Seccion { get; set; }
    }
}
