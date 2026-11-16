using System;
using System.Collections.Generic;
using System.Linq;
using EduNexus.UI.Models.Identity;

namespace EduNexus.UI.Models
{
    // Datos de un estudiante que importan para repartirlo entre secciones.
    public class PerfilDistribucion
    {
        // "F", "M", "Otro" o null si no hay dato de género.
        public string Genero { get; set; }

        // Tiene adecuación o discapacidad (prematrícula) o una adecuación activa.
        public bool TieneNecesidades { get; set; }

        public string GeneroTexto
        {
            get { return Genero ?? "Sin dato"; }
        }
    }

    // Composición actual de una sección.
    public class EstadoSeccionDistribucion
    {
        public int IdSeccion { get; set; }
        public string Etiqueta { get; set; }
        public int Cupo { get; set; }
        public int Total { get; set; }
        public int Femenino { get; set; }
        public int Masculino { get; set; }
        public int Otro { get; set; }
        public int SinGenero { get; set; }
        public int ConNecesidades { get; set; }

        public int Libres
        {
            get { return Math.Max(0, Cupo - Total); }
        }

        public int CantidadDeGenero(string genero)
        {
            switch (genero)
            {
                case "F": return Femenino;
                case "M": return Masculino;
                case "Otro": return Otro;
                default: return SinGenero;
            }
        }

        // "F 10 · M 9 · Otro 0 · Necesidades 2"
        public string Resumen
        {
            get
            {
                return "F " + Femenino + " · M " + Masculino + " · Otro " + Otro
                    + (SinGenero > 0 ? " · Sin dato " + SinGenero : "")
                    + " · Necesidades " + ConNecesidades;
            }
        }
    }

    public class ResultadoDistribucion
    {
        public EstadoSeccionDistribucion Seccion { get; set; }
        public string Motivo { get; set; }
    }

    // Distribución equilibrada de estudiantes entre las secciones de un grado.
    // Criterio (en este orden):
    //   0. Nunca se elige una sección llena (cupo libre = 0).
    //   a. Si el estudiante tiene necesidades (adecuación o discapacidad), la sección
    //      con MENOS estudiantes con necesidades, para que ninguna los concentre.
    //   b. Si se conoce su género, la sección donde la proporción de su género,
    //      contándolo a él, quede MÁS BAJA: (mismo género + 1) / (total + 1).
    //   c. La sección con MÁS cupo libre.
    //   d. Empate total: la de nombre menor (orden estable).
    public static class DistribucionSecciones
    {
        public static string NormalizarGenero(string genero)
        {
            string g = HorarioServicio.SinTildes((genero ?? "").Trim()).ToLowerInvariant();
            if (g == "") return null;
            if (g == "f" || g.StartsWith("fem") || g == "mujer") return "F";
            if (g == "m" || g.StartsWith("mas") || g == "hombre") return "M";
            return "Otro";
        }

        // Perfil de cada usuario: género y adecuación/discapacidad de su última prematrícula
        // (emparejada por cédula, sin importar el formato) + adecuaciones activas.
        public static Dictionary<string, PerfilDistribucion> Perfiles(EduNexusDbContext db, IEnumerable<UsuarioEntity> usuarios)
        {
            var lista = usuarios.Where(u => u != null).ToList();
            var resultado = new Dictionary<string, PerfilDistribucion>();
            if (!lista.Any()) return resultado;

            var ultimas = db.Prematriculas
                .Select(p => new { p.identificacion, p.genero, p.tiene_adecuacion, p.tiene_discapacidad, p.fecha_prematricula, p.id_prematricula })
                .ToList()
                .GroupBy(p => Cedula.Normalizar(p.identificacion) ?? (p.identificacion ?? "").Trim())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.fecha_prematricula).ThenByDescending(p => p.id_prematricula).First());

            var conAdecuacionActiva = new HashSet<string>(
                db.AdecuacionesEstudiantes
                    .Select(a => new { a.fk_id_usuario, a.estado })
                    .ToList()
                    .Where(a => a.fk_id_usuario != null && string.Equals((a.estado ?? "").Trim(), "Activa", StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.fk_id_usuario));

            foreach (var u in lista)
            {
                if (resultado.ContainsKey(u.id_usuario)) continue;
                string clave = Cedula.Normalizar(u.identificacion) ?? (u.identificacion ?? "").Trim();
                var p = clave != "" && ultimas.ContainsKey(clave) ? ultimas[clave] : null;
                resultado[u.id_usuario] = new PerfilDistribucion
                {
                    Genero = p != null ? NormalizarGenero(p.genero) : null,
                    TieneNecesidades = (p != null && (p.tiene_adecuacion || p.tiene_discapacidad))
                                       || conAdecuacionActiva.Contains(u.id_usuario)
                };
            }
            return resultado;
        }

        // Composición (por género y necesidades) de las secciones indicadas.
        public static Dictionary<int, EstadoSeccionDistribucion> EstadoDeSecciones(EduNexusDbContext db, IEnumerable<SeccionEntity> secciones)
        {
            var lista = secciones.Where(s => s != null).ToList();
            var ids = lista.Select(s => s.id_seccion).Distinct().ToList();
            var etiquetas = HorarioServicio.EtiquetasDeSecciones(db);

            var asignaciones = db.SeccionesEstudiantes
                .Where(e => ids.Contains(e.fk_id_seccion))
                .Select(e => new { e.fk_id_seccion, e.fk_id_usuario })
                .ToList();
            var idsUsuarios = asignaciones.Select(a => a.fk_id_usuario).Distinct().ToList();
            var usuarios = db.Usuarios.Where(u => idsUsuarios.Contains(u.id_usuario)).ToList();
            var perfiles = Perfiles(db, usuarios);

            var resultado = new Dictionary<int, EstadoSeccionDistribucion>();
            foreach (var s in lista)
            {
                if (resultado.ContainsKey(s.id_seccion)) continue;
                var estado = new EstadoSeccionDistribucion
                {
                    IdSeccion = s.id_seccion,
                    Etiqueta = etiquetas.ContainsKey(s.id_seccion) ? etiquetas[s.id_seccion] : s.nombre,
                    Cupo = s.cupo
                };
                foreach (var a in asignaciones.Where(x => x.fk_id_seccion == s.id_seccion))
                {
                    PerfilDistribucion p;
                    if (!perfiles.TryGetValue(a.fk_id_usuario ?? "", out p)) p = new PerfilDistribucion();
                    estado.Total++;
                    switch (p.Genero)
                    {
                        case "F": estado.Femenino++; break;
                        case "M": estado.Masculino++; break;
                        case "Otro": estado.Otro++; break;
                        default: estado.SinGenero++; break;
                    }
                    if (p.TieneNecesidades) estado.ConNecesidades++;
                }
                resultado[s.id_seccion] = estado;
            }
            return resultado;
        }

        // Elige la sección más equilibrada para el estudiante (null si todas están llenas).
        public static ResultadoDistribucion Elegir(PerfilDistribucion estudiante, IEnumerable<EstadoSeccionDistribucion> candidatas)
        {
            estudiante = estudiante ?? new PerfilDistribucion();
            var conCupo = candidatas.Where(c => c != null && c.Cupo > 0 && c.Libres > 0).ToList();
            if (!conCupo.Any()) return null;

            var ordenadas = conCupo.ToList();
            ordenadas.Sort((x, y) => Comparar(estudiante, x, y));
            var elegida = ordenadas[0];

            string motivo;
            if (ordenadas.Count == 1)
            {
                motivo = "única sección con cupo";
            }
            else
            {
                var segunda = ordenadas[1];
                if (CompararNecesidades(estudiante, elegida, segunda) != 0)
                    motivo = "menos estudiantes con necesidades (" + elegida.ConNecesidades + ")";
                else if (CompararGenero(estudiante, elegida, segunda) != 0)
                    motivo = "más equilibrio de género";
                else if (elegida.Libres != segunda.Libres)
                    motivo = "más cupo libre (" + elegida.Libres + ")";
                else
                    motivo = "secciones igual de equilibradas; se tomó la primera";
            }
            return new ResultadoDistribucion { Seccion = elegida, Motivo = motivo };
        }

        private static int Comparar(PerfilDistribucion e, EstadoSeccionDistribucion x, EstadoSeccionDistribucion y)
        {
            int r = CompararNecesidades(e, x, y);
            if (r != 0) return r;
            r = CompararGenero(e, x, y);
            if (r != 0) return r;
            r = y.Libres.CompareTo(x.Libres);
            if (r != 0) return r;
            r = string.Compare(x.Etiqueta, y.Etiqueta, StringComparison.CurrentCultureIgnoreCase);
            return r != 0 ? r : x.IdSeccion.CompareTo(y.IdSeccion);
        }

        private static int CompararNecesidades(PerfilDistribucion e, EstadoSeccionDistribucion x, EstadoSeccionDistribucion y)
        {
            return e.TieneNecesidades ? x.ConNecesidades.CompareTo(y.ConNecesidades) : 0;
        }

        // Compara (g+1)/(t+1) sin decimales: a/b vs c/d  <=>  a*d vs c*b
        private static int CompararGenero(PerfilDistribucion e, EstadoSeccionDistribucion x, EstadoSeccionDistribucion y)
        {
            if (e.Genero == null) return 0;
            long ax = x.CantidadDeGenero(e.Genero) + 1, bx = x.Total + 1;
            long ay = y.CantidadDeGenero(e.Genero) + 1, by = y.Total + 1;
            return (ax * by).CompareTo(ay * bx);
        }
    }
}
