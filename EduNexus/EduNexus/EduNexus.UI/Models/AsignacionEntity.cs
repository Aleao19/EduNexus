using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: asignaciones (una fila por estudiante; las filas de una misma tarea comparten id_grupo)
    public class AsignacionEntity
    {
        public int id_asignacion { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal porcentaje_nota { get; set; }
        public decimal calificacion { get; set; }
        public string estado { get; set; }
        public string fk_id_usuario { get; set; }
        public int fk_id_seccion { get; set; }
        public int? fk_id_materia { get; set; }

        // RAV-12-002
        public DateTime? fecha_apertura { get; set; }
        public DateTime? fecha_cierre { get; set; }
        public string id_grupo { get; set; }
        public string fk_id_docente { get; set; }
        // RAV-12-003
        public DateTime? fecha_entrega { get; set; }
        public string archivo_nombre { get; set; }
        public string archivo_ruta { get; set; }
    }
}