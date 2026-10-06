using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: secciones_estudiantes (estudiantes asignados a cada sección)
    public class SeccionEstudianteEntity
    {
        public int id_seccion_estudiante { get; set; }
        public int fk_id_seccion { get; set; }
        public string fk_id_usuario { get; set; }
        public DateTime fecha_asignacion { get; set; }
    }
}
