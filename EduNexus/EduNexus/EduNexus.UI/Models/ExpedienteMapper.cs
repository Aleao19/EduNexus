
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

            // Crear entidad respetando los nombres
            // definidos en ExpedienteEntity.cs

            ExpedienteEntity entidad = new ExpedienteEntity
            {
                fk_id_matricula = expediente.id_matricula,

                fk_id_usuario = id_usuario,

                correo = expediente.correo,

                grupo_sanguineo = expediente.grupo_sanguineo,

                alergias = expediente.alergias,

                contactoEmergencia = expediente.contactoEmergencia
            };

            return entidad;
        }
    }
}
