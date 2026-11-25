using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: encargados_estudiantes
    public class EncargadoEstudianteEntity
    {
        public int id_encargado_estudiante { get; set; }

        public string fk_id_encargado { get; set; }

        public string fk_id_estudiante { get; set; }

        public string parentesco { get; set; }

        public bool estado { get; set; }

        public DateTime fecha_registro { get; set; }
    }
}