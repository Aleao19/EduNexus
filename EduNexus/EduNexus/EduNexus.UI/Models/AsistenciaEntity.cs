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

        // Trazabilidad de la justificación (solo con estado "Ausencia justificada").
        // Requiere las columnas motivo_justificacion VARCHAR(500) NULL,
        // fk_id_justificado_por VARCHAR(128) NULL y fecha_justificacion DATETIME NULL.
        public string motivo_justificacion { get; set; }
        public string fk_id_justificado_por { get; set; }
        public DateTime? fecha_justificacion { get; set; }
    }
}
