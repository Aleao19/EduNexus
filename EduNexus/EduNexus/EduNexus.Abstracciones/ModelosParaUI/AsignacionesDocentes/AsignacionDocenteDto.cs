namespace EduNexus.Abstracciones.ModelosParaUI.AsignacionesDocentes
{
    // Una clase (sección + materia) a cargo de un docente: fila de secciones_materias
    public class AsignacionDocenteDto
    {
        public int id_asignacion_docente { get; set; }
        public string id_docente { get; set; }
        public int id_materia { get; set; }
        public int id_seccion { get; set; }

        // Solo para mostrar en la ficha del docente
        public string materia { get; set; }
        public string seccion { get; set; }
        public int estudiantes_matriculados { get; set; }
        public int cupo { get; set; }
        public int bloques_horario { get; set; }
        public System.DateTime? fecha_asignacion { get; set; }
    }
}
