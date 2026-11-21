
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

        // Tiene adecuación o discapacidad en prematrícula
        // o una adecuación activa.
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

        public string Resumen
        {
            get
            {
                return "F " + Femenino +
                    " · M " + Masculino +
                    " · Otro " + Otro +
                    (SinGenero > 0 ? " · Sin dato " + SinGenero : "") +
                    " · Necesidades " + ConNecesidades;
            }
        }
    }

    public class ResultadoDistribucion
    {
        public EstadoSeccionDistribucion Seccion { get; set; }
        public string Motivo { get; set; }
    }

    // Distribución equilibrada de estudiantes entre las secciones.
    //
    // PRIORIDADES:
    // 1. Respetar el cupo disponible.
    // 2. Equilibrar la cantidad total de estudiantes.
    // 3. Procurar una distribución de sexo cercana al 60/40,
    //    siempre que la composición real del grado lo permita.
    // 4. Distribuir equitativamente a los estudiantes con necesidades.
    // 5. Desempatar aleatoriamente.
    //
    // Los 15 estudiantes son una referencia de conformación
    // de las secciones, no una condición para aprobar matrículas.
    //
    // No se utilizan rendimiento académico ni convivencia.

    public static class DistribucionSecciones
    {
        private static readonly Random Aleatorio = new Random();
        private static readonly object BloqueoAleatorio = new object();

        private const double OBJETIVO_SEXO = 0.60;
        private const double TOLERANCIA = 0.0000001;

        public static string NormalizarGenero(string genero)
        {
            string g = HorarioServicio.SinTildes(
                (genero ?? "").Trim()).ToLowerInvariant();

            if (g == "")
                return null;

            if (g == "f" || g.StartsWith("fem") || g == "mujer")
                return "F";

            if (g == "m" || g.StartsWith("mas") || g == "hombre")
                return "M";

            return "Otro";
        }

        // Obtiene el perfil de cada usuario utilizando su última
        // prematrícula y sus adecuaciones activas.
        public static Dictionary<string, PerfilDistribucion> Perfiles(
            EduNexusDbContext db,
            IEnumerable<UsuarioEntity> usuarios)
        {
            var lista = (usuarios ?? Enumerable.Empty<UsuarioEntity>())
                .Where(u => u != null)
                .ToList();

            var resultado =
                new Dictionary<string, PerfilDistribucion>();

            if (!lista.Any())
                return resultado;

            var ultimas = db.Prematriculas
                .Select(p => new
                {
                    p.identificacion,
                    p.genero,
                    p.tiene_adecuacion,
                    p.tiene_discapacidad,
                    p.fecha_prematricula,
                    p.id_prematricula
                })
                .ToList()
                .GroupBy(p =>
                    Cedula.Normalizar(p.identificacion) ??
                    (p.identificacion ?? "").Trim())
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(p => p.fecha_prematricula)
                          .ThenByDescending(p => p.id_prematricula)
                          .First());

            var conAdecuacionActiva = new HashSet<string>(
                db.AdecuacionesEstudiantes
                    .Select(a => new
                    {
                        a.fk_id_usuario,
                        a.estado
                    })
                    .ToList()
                    .Where(a =>
                        a.fk_id_usuario != null &&
                        string.Equals(
                            (a.estado ?? "").Trim(),
                            "Activa",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.fk_id_usuario));

            foreach (var u in lista)
            {
                if (resultado.ContainsKey(u.id_usuario))
                    continue;

                string clave =
                    Cedula.Normalizar(u.identificacion) ??
                    (u.identificacion ?? "").Trim();

                var p = clave != "" && ultimas.ContainsKey(clave)
                    ? ultimas[clave]
                    : null;

                resultado[u.id_usuario] =
                    new PerfilDistribucion
                    {
                        Genero = p != null
                            ? NormalizarGenero(p.genero)
                            : null,

                        TieneNecesidades =
                            (p != null &&
                            (p.tiene_adecuacion ||
                             p.tiene_discapacidad))
                            || conAdecuacionActiva.Contains(u.id_usuario)
                    };
            }

            return resultado;
        }

        // Calcula la composición actual de las secciones indicadas.
        public static Dictionary<int, EstadoSeccionDistribucion>
            EstadoDeSecciones(
                EduNexusDbContext db,
                IEnumerable<SeccionEntity> secciones)
        {
            var lista = (secciones ?? Enumerable.Empty<SeccionEntity>())
                .Where(s => s != null)
                .ToList();

            var ids = lista
                .Select(s => s.id_seccion)
                .Distinct()
                .ToList();

            var etiquetas =
                HorarioServicio.EtiquetasDeSecciones(db);

            var asignaciones = db.SeccionesEstudiantes
                .Where(e => ids.Contains(e.fk_id_seccion))
                .Select(e => new
                {
                    e.fk_id_seccion,
                    e.fk_id_usuario
                })
                .ToList();

            var idsUsuarios = asignaciones
                .Select(a => a.fk_id_usuario)
                .Distinct()
                .ToList();

            var usuarios = db.Usuarios
                .Where(u => idsUsuarios.Contains(u.id_usuario))
                .ToList();

            var perfiles = Perfiles(db, usuarios);

            var resultado =
                new Dictionary<int, EstadoSeccionDistribucion>();

            foreach (var s in lista)
            {
                if (resultado.ContainsKey(s.id_seccion))
                    continue;

                var estado = new EstadoSeccionDistribucion
                {
                    IdSeccion = s.id_seccion,

                    Etiqueta = etiquetas.ContainsKey(s.id_seccion)
                        ? etiquetas[s.id_seccion]
                        : s.nombre,

                    Cupo = s.cupo
                };

                foreach (var a in asignaciones.Where(
                    x => x.fk_id_seccion == s.id_seccion))
                {
                    PerfilDistribucion p;

                    if (!perfiles.TryGetValue(
                        a.fk_id_usuario ?? "",
                        out p))
                    {
                        p = new PerfilDistribucion();
                    }

                    estado.Total++;

                    switch (p.Genero)
                    {
                        case "F":
                            estado.Femenino++;
                            break;

                        case "M":
                            estado.Masculino++;
                            break;

                        case "Otro":
                            estado.Otro++;
                            break;

                        default:
                            estado.SinGenero++;
                            break;
                    }

                    if (p.TieneNecesidades)
                        estado.ConNecesidades++;
                }

                resultado[s.id_seccion] = estado;
            }

            return resultado;
        }

        // Elige la sección que produce la distribución más equilibrada.
        // Devuelve null si todas las secciones están llenas.
        public static ResultadoDistribucion Elegir(
            PerfilDistribucion estudiante,
            IEnumerable<EstadoSeccionDistribucion> candidatas)
        {
            estudiante = estudiante ?? new PerfilDistribucion();

            if (candidatas == null)
                return null;

            // Se conservan todas las secciones para evaluar
            // el equilibrio general del grado y calendario.
            var todasLasSecciones = candidatas
                .Where(c => c != null && c.Cupo > 0)
                .GroupBy(c => c.IdSeccion)
                .Select(g => g.First())
                .ToList();

            if (!todasLasSecciones.Any())
                return null;

            // Solo pueden recibir estudiantes las secciones con cupo.
            var conCupo = todasLasSecciones
                .Where(c => c.Libres > 0)
                .ToList();

            if (!conCupo.Any())
                return null;

            // PRIORIDAD 1:
            // Evaluar cómo queda la cantidad total de estudiantes
            // en todas las secciones después de cada posible asignación.
            double mejorPuntajeCantidad = conCupo
                .Min(c => PuntajeCantidad(c, todasLasSecciones));

            var mejoresPorCantidad = conCupo
                .Where(c =>
                    Math.Abs(
                        PuntajeCantidad(c, todasLasSecciones) -
                        mejorPuntajeCantidad) < TOLERANCIA)
                .ToList();

            bool cantidadDiferencio =
                mejoresPorCantidad.Count < conCupo.Count;

            // PRIORIDAD 2:
            // Procurar el equilibrio de sexo.
            double mejorPuntajeSexo = mejoresPorCantidad
                .Min(c => PuntajeSexo(
                    estudiante,
                    c,
                    todasLasSecciones));

            var mejoresPorSexo = mejoresPorCantidad
                .Where(c =>
                    Math.Abs(
                        PuntajeSexo(
                            estudiante,
                            c,
                            todasLasSecciones) -
                        mejorPuntajeSexo) < TOLERANCIA)
                .ToList();

            bool sexoDiferencio =
                mejoresPorSexo.Count < mejoresPorCantidad.Count;

            // PRIORIDAD 3:
            // Procurar una distribución equitativa de estudiantes
            // con necesidades entre todas las secciones.
            double mejorPuntajeNecesidades = mejoresPorSexo
                .Min(c => PuntajeNecesidades(
                    estudiante,
                    c,
                    todasLasSecciones));

            var finalistas = mejoresPorSexo
                .Where(c =>
                    Math.Abs(
                        PuntajeNecesidades(
                            estudiante,
                            c,
                            todasLasSecciones) -
                        mejorPuntajeNecesidades) < TOLERANCIA)
                .ToList();

            bool necesidadesDiferencio =
                finalistas.Count < mejoresPorSexo.Count;

            // PRIORIDAD 4:
            // Desempate aleatorio entre secciones equivalentes.
            EstadoSeccionDistribucion elegida;

            lock (BloqueoAleatorio)
            {
                elegida = finalistas[
                    Aleatorio.Next(finalistas.Count)];
            }

            string motivo;

            if (conCupo.Count == 1)
            {
                motivo = "única sección con cupo";
            }
            else if (cantidadDiferencio)
            {
                motivo = "equilibrio en la cantidad de estudiantes";
            }
            else if (sexoDiferencio && necesidadesDiferencio)
            {
                motivo = "equilibrio de sexo y necesidades educativas";
            }
            else if (sexoDiferencio)
            {
                motivo = "equilibrio de sexo";
            }
            else if (necesidadesDiferencio)
            {
                motivo = "equilibrio de necesidades educativas";
            }
            else
            {
                motivo = "secciones equivalentes; selección aleatoria";
            }

            return new ResultadoDistribucion
            {
                Seccion = elegida,
                Motivo = motivo
            };
        }

        // Calcula el desequilibrio en la cantidad total
        // después de asignar al estudiante a una sección.
        //
        // Se utiliza la suma de desviaciones cuadráticas respecto
        // al promedio. Un resultado menor representa mayor equilibrio.
        private static double PuntajeCantidad(
            EstadoSeccionDistribucion candidata,
            List<EstadoSeccionDistribucion> secciones)
        {
            double totalGeneral =
                secciones.Sum(s => (double)s.Total) + 1;

            double promedio =
                totalGeneral / secciones.Count;

            double puntaje = 0;

            foreach (var s in secciones)
            {
                double cantidad = s.Total;

                if (s.IdSeccion == candidata.IdSeccion)
                    cantidad++;

                double diferencia = cantidad - promedio;

                puntaje += diferencia * diferencia;
            }

            return puntaje;
        }

        // Calcula el objetivo de distribución por sexo.
        //
        // Si el sexo mayoritario representa hasta el 60 %,
        // se procura una distribución 60/40.
        //
        // Si supera el 60 %, se respeta la proporción real
        // de estudiantes del grado.
        private static double ObtenerObjetivoFemenino(
            PerfilDistribucion estudiante,
            List<EstadoSeccionDistribucion> secciones)
        {
            double femenino = secciones.Sum(
                s => (double)s.Femenino);

            double masculino = secciones.Sum(
                s => (double)s.Masculino);

            if (estudiante.Genero == "F")
                femenino++;

            if (estudiante.Genero == "M")
                masculino++;

            double total = femenino + masculino;

            if (total == 0)
                return 0.50;

            if (Math.Abs(femenino - masculino) < TOLERANCIA)
                return 0.50;

            double proporcionFemenina = femenino / total;
            double proporcionMasculina = masculino / total;

            if (proporcionFemenina > proporcionMasculina)
            {
                return proporcionFemenina <= OBJETIVO_SEXO
                    ? OBJETIVO_SEXO
                    : proporcionFemenina;
            }

            return proporcionMasculina <= OBJETIVO_SEXO
                ? 1.0 - OBJETIVO_SEXO
                : proporcionFemenina;
        }

        // Calcula qué tan cerca queda cada sección del objetivo
        // de distribución por sexo después de recibir al estudiante.
        private static double PuntajeSexo(
            PerfilDistribucion estudiante,
            EstadoSeccionDistribucion candidata,
            List<EstadoSeccionDistribucion> todasLasSecciones)
        {
            double objetivoFemenino =
                ObtenerObjetivoFemenino(
                    estudiante,
                    todasLasSecciones);

            double puntaje = 0;

            foreach (var s in todasLasSecciones)
            {
                double femenino = s.Femenino;
                double masculino = s.Masculino;

                if (s.IdSeccion == candidata.IdSeccion)
                {
                    if (estudiante.Genero == "F")
                        femenino++;

                    if (estudiante.Genero == "M")
                        masculino++;
                }

                double totalConSexo = femenino + masculino;

                // No se penaliza una sección sin datos F/M.
                if (totalConSexo == 0)
                    continue;

                double proporcionFemenina =
                    femenino / totalConSexo;

                double diferencia =
                    proporcionFemenina - objetivoFemenino;

                puntaje += diferencia * diferencia;
            }

            return puntaje;
        }

        // Calcula el desequilibrio en la cantidad de estudiantes
        // con necesidades después de asignar al estudiante.
        // El objetivo es aproximarse a una distribución 3 y 3.
        private static double PuntajeNecesidades(
            PerfilDistribucion estudiante,
            EstadoSeccionDistribucion candidata,
            List<EstadoSeccionDistribucion> todasLasSecciones)
        {
            double totalNecesidades =
                todasLasSecciones.Sum(
                    s => (double)s.ConNecesidades);

            if (estudiante.TieneNecesidades)
                totalNecesidades++;

            double promedioNecesidades =
                totalNecesidades / todasLasSecciones.Count;

            double puntaje = 0;

            foreach (var s in todasLasSecciones)
            {
                double cantidad = s.ConNecesidades;

                if (s.IdSeccion == candidata.IdSeccion &&
                    estudiante.TieneNecesidades)
                {
                    cantidad++;
                }

                double diferencia =
                    cantidad - promedioNecesidades;

                puntaje += diferencia * diferencia;
            }

            return puntaje;
        }
    }
}
