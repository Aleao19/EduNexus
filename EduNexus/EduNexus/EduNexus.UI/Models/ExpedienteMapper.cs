
using EduNexus.Abstracciones.ModelosParaUI.Expedientes;
using EduNexus.UI.Models.Identity;

namespace EduNexus.UI.Models
{
    // =========================================
    // REXP-03-001
    // CONVERSION DE DTO A ENTIDAD
    // =========================================

    public static class ExpedienteMapper
    {
        public static ExpedienteEntity ConvertirAEntidad(
            ExpedienteDto expediente,
            string id_usuario)
        {
            if (expediente == null)
            {
                throw new System.ArgumentNullException(
                    "expediente"
                );
            }

            if (string.IsNullOrWhiteSpace(id_usuario))
            {
                throw new System.ArgumentException(
                    "Debe proporcionar el ID del usuario estudiante.",
                    "id_usuario"
                );
            }

            // Solo se guardan los datos propios del expediente;
            // los datos personales quedan en la prematrícula.
            return new ExpedienteEntity
            {
                fk_id_matricula = expediente.id_matricula,
                fk_id_usuario = id_usuario,
                correo = Limpiar(expediente.correo),
                grupo_sanguineo = Limpiar(expediente.grupo_sanguineo),
                alergias = Limpiar(expediente.alergias),
                contactoEmergencia = Limpiar(expediente.contactoEmergencia)
            };
        }

        // Copia al expediente existente los campos editables propios del expediente.
        public static void ActualizarEntidad(
            ExpedienteEntity entidad,
            ExpedienteDto expediente)
        {
            entidad.correo = Limpiar(expediente.correo);
            entidad.grupo_sanguineo = Limpiar(expediente.grupo_sanguineo);
            entidad.alergias = Limpiar(expediente.alergias);
            entidad.contactoEmergencia = Limpiar(expediente.contactoEmergencia);
        }

        public static string Limpiar(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }
    }
}
