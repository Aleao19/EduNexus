using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: asistencia
    public class AsistenciaEntity
    {
        public int id_asistencia { get; set; }
        public DateTime fecha { get; set; }
        public string estado { get; set; }
        public int fk_id_seccion { get; set; }
        public int fk_id_materia { get; set; }
        public string fk_id_usuario { get; set; }
        public string fk_id_registrado_por { get; set; }
        public DateTime fecha_registro { get; set; }
    }
}