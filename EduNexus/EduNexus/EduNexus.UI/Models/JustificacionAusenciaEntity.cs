using System;

namespace EduNexus.UI.Models.Identity
{
    // RASI-10-004
    // Solicitudes de justificación de ausencias
    // enviadas por los padres de familia.

    public class JustificacionAusenciaEntity
    {
        public int id_justificacion { get; set; }

        public int fk_id_asistencia { get; set; }

        public string fk_id_estudiante { get; set; }

        public string fk_id_encargado { get; set; }

        public string motivo { get; set; }

        public DateTime fecha_envio { get; set; }

        public string estado { get; set; }

        public string fk_id_docente { get; set; }

        public DateTime? fecha_revision { get; set; }

        public string observaciones { get; set; }
    }
}