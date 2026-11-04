using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: secciones_materias (materias que se imparten en cada sección)
    public class SeccionMateriaEntity
    {
        public int id_seccion_materia { get; set; }
        public int fk_id_seccion { get; set; }
        public int fk_id_materia { get; set; }
        public DateTime fecha_asignacion { get; set; }
    }
}