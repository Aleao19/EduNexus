using System;
using System.Linq;
using EduNexus.UI.Models.Identity;

namespace EduNexus.UI.Models
{
    public static class NotificacionServicio
    {
        public static void GenerarNotificacionAusencia(
            EduNexusDbContext db,
            AsistenciaEntity asistencia)
        {
            if (asistencia == null)
            {
                return;
            }

            if (asistencia.estado != "Ausencia no justificada" &&
                asistencia.estado != "Ausencia justificada")
            {
                return;
            }

            if (asistencia.id_asistencia <= 0)
            {
                throw new InvalidOperationException(
                    "La asistencia debe estar registrada.");
            }

            // Obtener información del estudiante.
            var estudiante = db.Usuarios.FirstOrDefault(
                u => u.id_usuario == asistencia.fk_id_usuario);

            if (estudiante == null)
            {
                return;
            }

            // Obtener la materia.
            var materia = db.Materias.FirstOrDefault(
                m => m.id_materia == asistencia.fk_id_materia);

            string nombreEstudiante =
                estudiante.nombre + " " +
                estudiante.apellido1 + " " +
                estudiante.apellido2;

            string nombreMateria = materia != null
                ? materia.nombre
                : "Materia no especificada";

            // Obtener los encargados activos del estudiante.
            var encargados = (
                from relacion in db.EncargadosEstudiantes
                join usuario in db.Usuarios
                    on relacion.fk_id_encargado
                    equals usuario.id_usuario
                where relacion.fk_id_estudiante ==
                      asistencia.fk_id_usuario
                      && relacion.estado
                      && usuario.estado
                select relacion.fk_id_encargado
            ).Distinct().ToList();

            foreach (string idEncargado in encargados)
            {
                // Evitar notificaciones duplicadas.
                bool existe = db.Notificaciones.Any(n =>
                    n.fk_id_asistencia ==
                        asistencia.id_asistencia &&
                    n.fk_id_destinatario == idEncargado);

                bool existeLocal = db.Notificaciones.Local.Any(n =>
                    n.fk_id_asistencia ==
                        asistencia.id_asistencia &&
                    n.fk_id_destinatario == idEncargado);

                if (existe || existeLocal)
                {
                    continue;
                }

                var notificacion = new NotificacionEntity
                {
                    fk_id_asistencia =
                        asistencia.id_asistencia,

                    fk_id_estudiante =
                        asistencia.fk_id_usuario,

                    fk_id_destinatario =
                        idEncargado,

                    titulo = "Notificación de ausencia",

                    mensaje =
                        "Se ha registrado una ausencia de " +
                        nombreEstudiante.Trim() +
                        " en la materia " +
                        nombreMateria +
                        ", correspondiente al día " +
                        asistencia.fecha.ToString("dd/MM/yyyy") +
                        ". Puede consultar el registro de asistencia " +
                        "y enviar una justificación si corresponde.",

                    fecha_creacion = DateTime.Now,

                    leida = false,

                    fecha_lectura = null
                };

                db.Notificaciones.Add(notificacion);
            }
        }
    }
}