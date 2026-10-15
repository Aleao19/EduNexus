using System;

namespace EduNexus.UI.Models.Identity
{
    public class HorarioEntity
    {
        public int id_horario { get; set; }
        public DateTime hora_inicio { get; set; }
        public DateTime hora_fin { get; set; }
        public string dia_semana { get; set; }
        public int fk_seccion { get; set; }
        public int fk_materia { get; set; }
    }
}