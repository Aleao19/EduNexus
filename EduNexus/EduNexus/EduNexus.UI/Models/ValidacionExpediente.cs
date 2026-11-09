
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
        public const string EstadoAprobada = "Aprobada";

        public static readonly string[] GruposSanguineos =
        {
            "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"
        };

        public static readonly string[] Generos =
        {
            "Femenino", "Masculino", "Otro"
        };

        // =========================================
        // VALIDAR RELACIONES DEL EXPEDIENTE
        // La prematrícula debe existir y estar aprobada, y el
        // estudiante no puede tener otro expediente (ni por
        // matrícula, ni por identificación, ni por usuario).
        // =========================================

        public static List<string> ValidarRelacion(
            EduNexusDbContext db,
            int id_matricula,
            string id_usuario)
        {
            List<string> errores = new List<string>();

            PrematriculaEntity prematricula = id_matricula > 0
                ? db.Prematriculas.FirstOrDefault(p => p.id_prematricula == id_matricula)
                : null;

            if (prematricula == null)
            {
                errores.Add("Debe seleccionar una matrícula válida.");
            }
            else
            {
                if (prematricula.estado != EstadoAprobada)
                {
                    errores.Add("Solo se puede crear el expediente de una matrícula aprobada.");
                }

                string identificacion = prematricula.identificacion;

                // Matrículas del mismo estudiante (nuevo ingreso y renovaciones)
                List<int> idsMatriculas = db.Prematriculas
                    .Where(p => p.identificacion == identificacion)
                    .Select(p => p.id_prematricula)
                    .ToList();

                bool existeExpediente = db.Expedientes
                    .Any(e => idsMatriculas.Contains(e.fk_id_matricula));

                if (existeExpediente)
                {
                    errores.Add("El estudiante ya tiene un expediente registrado.");
                }
            }

            if (string.IsNullOrWhiteSpace(id_usuario))
            {
                errores.Add("El estudiante no tiene un usuario con rol Estudiante. Apruebe su prematrícula para crearlo.");
            }
            else if (db.Expedientes.Any(e => e.fk_id_usuario == id_usuario))
            {
                errores.Add("El usuario del estudiante ya tiene un expediente registrado.");
            }

            return errores.Distinct().ToList();
        }

        // =========================================
        // BUSCAR USUARIO DEL ESTUDIANTE
        // Solo se acepta un usuario con rol Estudiante.
        // Si hay coincidencias duplicadas no se elige una al azar.
        // =========================================

        public static UsuarioEntity BuscarUsuarioEstudiante(
            EduNexusDbContext db,
            string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return null;
            }

            string idRol = IdRolEstudiante(db);

            if (idRol == null)
            {
                return null;
            }

            var coincidencias = db.Usuarios
                .Where(u => u.identificacion == identificacion && u.rol == idRol)
                .Take(2)
                .ToList();

            return coincidencias.Count == 1 ? coincidencias[0] : null;
        }

        public static string ObtenerIdUsuarioEstudiante(
            EduNexusDbContext db,
            string identificacion)
        {
            UsuarioEntity usuario = BuscarUsuarioEstudiante(db, identificacion);
            return usuario != null ? usuario.id_usuario : null;
        }

        public static string IdRolEstudiante(EduNexusDbContext db)
        {
            return db.Roles.ToList()
                .Where(r => RolesSistema.Es(r.nombre, RolesSistema.Estudiante))
                .Select(r => r.id_rol)
                .FirstOrDefault();
        }
    }
}
