using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Horarios
{
    public class HorariosDto
    {
        public int id_horario { get; set; }
        [Required]
        [DisplayName("Hora inicio")]
        public TimeSpan hora_inicio { get; set; }
        [Required]
        [DisplayName("Hora fin")]
        public TimeSpan hora_fin { get; set; }
        [Required]
        [DisplayName("Día de la semana")]
        public string dia_semana { get; set; }
        [Required]
        [DisplayName("Sección")]
        public int seccion { get; set; }
        [Required]
        [DisplayName("Materia")]
        public int materia { get; set; }
    }
}
