using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: notificaciones
    public class NotificacionEntity
    {
        public int id_notificacion { get; set; }

        public int fk_id_asistencia { get; set; }

        public string fk_id_estudiante { get; set; }

        public string fk_id_destinatario { get; set; }

        public string titulo { get; set; }

        public string mensaje { get; set; }

        public DateTime fecha_creacion { get; set; }

        public bool leida { get; set; }

        public DateTime? fecha_lectura { get; set; }
    }
}