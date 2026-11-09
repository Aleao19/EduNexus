using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EduNexus.UI.Models
{
    // Roles precargados del sistema. No se crean roles nuevos desde la aplicación:
    // cada rol tiene permisos fijos definidos en el código.
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string CoordinadorAcademico = "Coordinador Academico";
        public const string Director = "Director";
        public const string Docente = "Docente";
        public const string Estudiante = "Estudiante";
        public const string Padre = "Padre";

        // Descripción de lo que puede hacer cada rol (se muestra en el listado de roles).
        public static readonly Dictionary<string, string> Permisos = new Dictionary<string, string>
        {
            { Normalizar(Administrador), "Acceso total: usuarios, grados, secciones, materias, docentes, horarios, prematrículas, expedientes y bitácora." },
            { Normalizar(CoordinadorAcademico), "Gestión de horarios y asignación de estudiantes a secciones." },
            { Normalizar(Director), "Consulta del dashboard y del aula virtual de todas las clases (solo lectura)." },
            { Normalizar(Docente), "Aula virtual de sus clases, registro de asistencia y su horario." },
            { Normalizar(Estudiante), "Aula virtual de sus clases y su horario." },
            { Normalizar(Padre), "Consulta del dashboard y realización de la prematrícula de sus hijos (es el encargado del estudiante)." }
        };

        // Compara nombres de rol sin importar mayúsculas, tildes ni espacios.
        public static string Normalizar(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol)) return "";
            string d = rol.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        public static bool Es(string nombreRol, string rol)
        {
            return Normalizar(nombreRol) == Normalizar(rol);
        }

        public static bool Es(UsuarioDto usuario, params string[] roles)
        {
            return usuario != null && roles.Any(r => Es(usuario.nombreRol, r));
        }

        public static string DescripcionPermisos(string nombreRol)
        {
            string p;
            return Permisos.TryGetValue(Normalizar(nombreRol), out p) ? p : "Sin permisos asignados en el sistema.";
        }
    }
}
