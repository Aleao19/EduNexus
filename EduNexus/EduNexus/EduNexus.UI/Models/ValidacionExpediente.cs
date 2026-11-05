
using System.Collections.Generic;
using System.Linq;
using EduNexus.UI.Models.Identity;

namespace EduNexus.UI.Models
{
    // =========================================
    // REXP-03-001
    // VALIDACION DE EXPEDIENTES
    // =========================================

    public static class ValidacionExpediente
    {
        // =========================================
        // VALIDAR RELACIONES DEL EXPEDIENTE
        // =========================================

        public static List<string> ValidarRelacion(
            EduNexusDbContext db,
            int id_matricula,
            string id_usuario)
        {
            List<string> errores = new List<string>();

            // Validar identificador de matricula
            if (id_matricula <= 0)
            {
                errores.Add(
                    "Debe seleccionar una matricula valida."
                );
            }
            else
            {
                // Verificar que exista la matricula
                bool existeMatricula = db.Prematriculas
                    .Any(p => p.id_matricula == id_matricula);

                if (!existeMatricula)
                {
                    errores.Add(
                        "La matricula seleccionada no existe."
                    );
                }

                // Evitar expediente duplicado
                bool existeExpediente = db.Expedientes
                    .Any(e => e.fk_id_matricula == id_matricula);

                if (existeExpediente)
                {
                    errores.Add(
                        "La matricula ya tiene un expediente registrado."
                    );
                }
            }

            // Validar identificador del usuario
            if (string.IsNullOrWhiteSpace(id_usuario))
            {
                errores.Add(
                    "Debe identificar el usuario estudiante."
                );
            }
            else
            {
                // Verificar que exista el usuario
                bool existeUsuario = db.Usuarios
                    .Any(u => u.id_usuario == id_usuario);

                if (!existeUsuario)
                {
                    errores.Add(
                        "El usuario estudiante no existe."
                    );
                }
            }

            return errores;
        }

        // =========================================
        // BUSCAR USUARIO DEL ESTUDIANTE
        // =========================================

        public static string ObtenerIdUsuarioEstudiante(
            EduNexusDbContext db,
            string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return null;
            }

            // Buscar el usuario por identificacion.
            // No se utiliza el usuario administrador.
            // Si existen coincidencias duplicadas,
            // no se selecciona una arbitrariamente.

            var coincidencias = db.Usuarios
                .Where(u => u.identificacion == identificacion)
                .Select(u => u.id_usuario)
                .Take(2)
                .ToList();

            if (coincidencias.Count != 1)
            {
                return null;
            }

            return coincidencias[0];
        }
    }
}
