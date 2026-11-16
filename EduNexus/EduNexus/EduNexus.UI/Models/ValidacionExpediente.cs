
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

                // Matrículas del mismo estudiante (nuevo ingreso y renovaciones),
                // comparando la cédula normalizada (datos viejos sin guiones).
                List<int> idsMatriculas = PrematriculasDe(db, prematricula.identificacion)
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

            // La cédula se compara normalizada en memoria (datos viejos sin guiones).
            var coincidencias = db.Usuarios
                .Where(u => u.rol == idRol && u.identificacion != null)
                .ToList()
                .Where(u => Cedula.Iguales(u.identificacion, identificacion))
                .Take(2)
                .ToList();

            return coincidencias.Count == 1 ? coincidencias[0] : null;
        }

        // =========================================
        // PREMATRÍCULAS DE UN ESTUDIANTE (por cédula normalizada)
        // =========================================

        public static List<PrematriculaEntity> PrematriculasDe(
            EduNexusDbContext db,
            string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return new List<PrematriculaEntity>();
            }

            return db.Prematriculas
                .Where(p => p.identificacion != null)
                .ToList()
                .Where(p => Cedula.Iguales(p.identificacion, identificacion))
                .ToList();
        }

        // =========================================
        // EXPEDIENTE DE UN USUARIO ESTUDIANTE
        // Primero por fk_id_usuario; si no, por las prematrículas
        // con su misma cédula (expedientes viejos sin usuario).
        // =========================================

        public static ExpedienteEntity BuscarExpedienteDeUsuario(
            EduNexusDbContext db,
            UsuarioEntity usuario)
        {
            if (usuario == null)
            {
                return null;
            }

            string idUsuario = usuario.id_usuario;

            ExpedienteEntity expediente = db.Expedientes
                .Where(e => e.fk_id_usuario == idUsuario)
                .OrderBy(e => e.id_expediente)
                .FirstOrDefault();

            if (expediente != null)
            {
                return expediente;
            }

            List<int> idsMatriculas = PrematriculasDe(db, usuario.identificacion)
                .Select(p => p.id_prematricula)
                .ToList();

            if (!idsMatriculas.Any())
            {
                return null;
            }

            return db.Expedientes
                .Where(e => e.fk_id_usuario == null && idsMatriculas.Contains(e.fk_id_matricula))
                .OrderBy(e => e.id_expediente)
                .FirstOrDefault();
        }

        // Usuario estudiante dueño de un expediente (null si no se puede determinar).
        public static string IdUsuarioDeExpediente(
            EduNexusDbContext db,
            int idExpediente)
        {
            ExpedienteEntity expediente = db.Expedientes
                .FirstOrDefault(e => e.id_expediente == idExpediente);

            if (expediente == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(expediente.fk_id_usuario))
            {
                return expediente.fk_id_usuario;
            }

            int idMatricula = expediente.fk_id_matricula;

            PrematriculaEntity prematricula = db.Prematriculas
                .FirstOrDefault(p => p.id_prematricula == idMatricula);

            UsuarioEntity usuario = prematricula != null
                ? BuscarUsuarioEstudiante(db, prematricula.identificacion)
                : null;

            return usuario != null ? usuario.id_usuario : null;
        }

        // Iniciales para el avatar: primera letra del nombre y del primer apellido.
        public static string Iniciales(string nombre, string apellido1)
        {
            string iniciales = "";

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                iniciales += char.ToUpperInvariant(nombre.Trim()[0]);
            }

            if (!string.IsNullOrWhiteSpace(apellido1))
            {
                iniciales += char.ToUpperInvariant(apellido1.Trim()[0]);
            }

            return iniciales.Length > 0 ? iniciales : "?";
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
