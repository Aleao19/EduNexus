
using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: secciones_estudiantes
    // Representa la asignación de un estudiante a una sección.

    public class SeccionEstudianteEntity
    {
        // Identificador único de la asignación.
        public int id_seccion_estudiante { get; set; }

        // Identificador de la sección asignada.
        public int fk_id_seccion { get; set; }

        // Identificador del usuario estudiante.
        public string fk_id_usuario { get; set; }

        // Fecha en la que se asignó el estudiante.
        public DateTime fecha_asignacion { get; set; }
    }
}
