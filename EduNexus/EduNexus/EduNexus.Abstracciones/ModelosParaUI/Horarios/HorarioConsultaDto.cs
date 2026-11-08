using System;
using System.Collections.Generic;

namespace EduNexus.Abstracciones.ModelosParaUI.Horarios
{
    public class HorarioItemDto
    {
        public int id_horario { get; set; }
        public string dia_semana { get; set; }
        public TimeSpan hora_inicio { get; set; }
        public TimeSpan hora_fin { get; set; }
        public string inicio { get; set; }
        public string fin { get; set; }
        public int id_materia { get; set; }
        public string materia { get; set; }
        public string profesor { get; set; }
        public int id_seccion { get; set; }
        public string seccion { get; set; }
    }

    // Consulta, visualización y "Mi horario"
    public class HorarioConsultaDto
    {
        public int? id_seccion { get; set; }
        public string id_docente { get; set; }
        public string titulo { get; set; }
        public string mensaje { get; set; }
        public bool consultado { get; set; }
        public string q { get; set; }
        public int pagina { get; set; } = 1;
        public int totalPaginas { get; set; } = 1;
        public int totalRegistros { get; set; }
        public int tamanoPagina { get; set; }
        public List<HorarioItemDto> items { get; set; } = new List<HorarioItemDto>();
    }

    // RHOR-09-004: horario con cupos y estudiantes asignados
    public class HorarioAsignacionDto
    {
        public int id_seccion { get; set; }
        public string seccion { get; set; }
        public int cupo { get; set; }
        public int matriculados { get; set; }
        public int cupos_disponibles { get; set; }
        public List<string> estudiantes { get; set; } = new List<string>();
        public List<HorarioItemDto> items { get; set; } = new List<HorarioItemDto>();
    }
}