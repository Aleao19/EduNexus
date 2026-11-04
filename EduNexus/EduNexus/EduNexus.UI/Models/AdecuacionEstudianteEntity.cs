using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: adecuaciones_estudiantes (casos de adecuación de cada estudiante)
    public class AdecuacionEstudianteEntity
    {
        public int id_adecuacion_estudiante { get; set; }
        public int? fk_id_adecuacion { get; set; }
        public string fk_id_usuario { get; set; }
        public string descripcion { get; set; }
        public string estado { get; set; }
        public DateTime fecha_registro { get; set; }
    }
}
