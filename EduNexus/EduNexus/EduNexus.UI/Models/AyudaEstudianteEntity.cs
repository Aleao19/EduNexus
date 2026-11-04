using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: ayudas_estudiantes (casos de ayuda de cada estudiante)
    public class AyudaEstudianteEntity
    {
        public int id_ayuda_estudiante { get; set; }
        public string fk_id_usuario { get; set; }
        public int? fk_id_tipo_ayuda { get; set; }
        public string descripcion { get; set; }
        public string estado { get; set; }
        public DateTime fecha_registro { get; set; }
    }
}
