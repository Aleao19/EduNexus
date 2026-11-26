using System;
using System.Collections.Generic;
using System.Linq;

namespace EduNexus.Abstracciones.ModelosParaUI.Notas
{
    public class ResumenNotasDto
    {
        // ========================================
        // INFORMACIÓN DEL ESTUDIANTE
        // ========================================

        public string id_estudiante { get; set; }

        public string identificacion_estudiante { get; set; }

        public string nombre_estudiante { get; set; }

        // ========================================
        // INFORMACIÓN ACADÉMICA
        // ========================================

        public int id_grado { get; set; }

        public string nombre_grado { get; set; }

        public int id_seccion { get; set; }

        public string nombre_seccion { get; set; }

        public int id_materia { get; set; }

        public string nombre_materia { get; set; }

        public int id_calendario { get; set; }

        public string nombre_calendario { get; set; }

        public string periodo { get; set; }

        // ========================================
        // INFORMACIÓN DEL DOCENTE
        // ========================================

        public string id_docente { get; set; }

        public string nombre_docente { get; set; }

        // ========================================
        // CALIFICACIONES REGISTRADAS
        // ========================================

        public List<NotaDto> Notas { get; set; }

        // ========================================
        // TOTAL DE EVALUACIONES
        // ========================================

        public int TotalEvaluaciones
        {
            get
            {
                return Notas == null
                    ? 0
                    : Notas.Count;
            }
        }

        // ========================================
        // TOTAL DE CALIFICACIONES
        // ========================================

        public int TotalCalificaciones
        {
            get
            {
                return Notas == null
                    ? 0
                    : Notas.Count(n => n.nota.HasValue);
            }
        }

        // ========================================
        // PROMEDIO DE LA MATERIA
        // ========================================

        public decimal PromedioMateria
        {
            get
            {
                if (Notas == null ||
                    !Notas.Any(n => n.nota.HasValue))
                {
                    return 0m;
                }

                return Math.Round(
                    Notas
                        .Where(n => n.nota.HasValue)
                        .Average(n => n.nota.Value),
                    2
                );
            }
        }

        // ========================================
        // NOTA MÁS ALTA
        // ========================================

        public decimal NotaMayor
        {
            get
            {
                if (Notas == null ||
                    !Notas.Any(n => n.nota.HasValue))
                {
                    return 0m;
                }

                return Notas
                    .Where(n => n.nota.HasValue)
                    .Max(n => n.nota.Value);
            }
        }

        // ========================================
        // NOTA MÁS BAJA
        // ========================================

        public decimal NotaMenor
        {
            get
            {
                if (Notas == null ||
                    !Notas.Any(n => n.nota.HasValue))
                {
                    return 0m;
                }

                return Notas
                    .Where(n => n.nota.HasValue)
                    .Min(n => n.nota.Value);
            }
        }

        // ========================================
        // CANTIDAD DE EXÁMENES
        // ========================================

        public int TotalExamenes
        {
            get
            {
                return ContarPorTipo("Examen");
            }
        }

        // ========================================
        // CANTIDAD DE TAREAS
        // ========================================

        public int TotalTareas
        {
            get
            {
                return ContarPorTipo("Tarea");
            }
        }

        // ========================================
        // CANTIDAD DE PROYECTOS
        // ========================================

        public int TotalProyectos
        {
            get
            {
                return ContarPorTipo("Proyecto");
            }
        }

        // ========================================
        // CANTIDAD DE TRABAJOS COTIDIANOS
        // ========================================

        public int TotalTrabajosCotidianos
        {
            get
            {
                return ContarPorTipo("Trabajo cotidiano");
            }
        }

        // ========================================
        // CANTIDAD DE TRABAJOS EN CLASE
        // ========================================

        public int TotalTrabajosEnClase
        {
            get
            {
                return ContarPorTipo("Trabajo en clase");
            }
        }

        // ========================================
        // CANTIDAD DE EXPOSICIONES
        // ========================================

        public int TotalExposiciones
        {
            get
            {
                return ContarPorTipo("Exposición");
            }
        }

        // ========================================
        // CANTIDAD DE PRÁCTICAS
        // ========================================

        public int TotalPracticas
        {
            get
            {
                return ContarPorTipo("Práctica");
            }
        }

        // ========================================
        // OTRAS EVALUACIONES
        // ========================================

        public int TotalOtrasEvaluaciones
        {
            get
            {
                return ContarPorTipo("Otro");
            }
        }

        // ========================================
        // PROMEDIO DE EXÁMENES
        // ========================================

        public decimal? PromedioExamenes
        {
            get
            {
                return ObtenerPromedioPorTipo("Examen");
            }
        }

        // ========================================
        // PROMEDIO DE TAREAS
        // ========================================

        public decimal? PromedioTareas
        {
            get
            {
                return ObtenerPromedioPorTipo("Tarea");
            }
        }

        // ========================================
        // PROMEDIO DE PROYECTOS
        // ========================================

        public decimal? PromedioProyectos
        {
            get
            {
                return ObtenerPromedioPorTipo("Proyecto");
            }
        }

        // ========================================
        // PROMEDIO DE TRABAJO COTIDIANO
        // ========================================

        public decimal? PromedioTrabajoCotidiano
        {
            get
            {
                return ObtenerPromedioPorTipo(
                    "Trabajo cotidiano"
                );
            }
        }

        // ========================================
        // ÚLTIMA EVALUACIÓN REGISTRADA
        // ========================================

        public DateTime? UltimaEvaluacion
        {
            get
            {
                if (Notas == null ||
                    !Notas.Any(n =>
                        n.fecha_evaluacion.HasValue))
                {
                    return null;
                }

                return Notas
                    .Where(n =>
                        n.fecha_evaluacion.HasValue)
                    .Max(n => n.fecha_evaluacion);
            }
        }

        // ========================================
        // ÚLTIMO REGISTRO DE CALIFICACIONES
        // ========================================

        public DateTime? UltimoRegistro
        {
            get
            {
                if (Notas == null || !Notas.Any())
                {
                    return null;
                }

                return Notas.Max(n => n.fecha_registro);
            }
        }

        // ========================================
        // TOTAL DE OBSERVACIONES
        // ========================================

        public int TotalObservaciones
        {
            get
            {
                return Notas == null
                    ? 0
                    : Notas.Count(n =>
                        !string.IsNullOrWhiteSpace(
                            n.observaciones));
            }
        }

        // ========================================
        // VALIDAR EXISTENCIA DE CALIFICACIONES
        // ========================================

        public bool TieneNotas
        {
            get
            {
                return Notas != null &&
                    Notas.Any(n => n.nota.HasValue);
            }
        }

        // ========================================
        // CONTAR EVALUACIONES POR TIPO
        // ========================================

        private int ContarPorTipo(string tipo)
        {
            if (Notas == null)
            {
                return 0;
            }

            return Notas.Count(n =>
                string.Equals(
                    n.tipo_evaluacion,
                    tipo,
                    StringComparison.OrdinalIgnoreCase
                ));
        }

        // ========================================
        // CALCULAR PROMEDIO POR TIPO
        // ========================================

        private decimal? ObtenerPromedioPorTipo(
            string tipo)
        {
            if (Notas == null)
            {
                return null;
            }

            var calificaciones = Notas
                .Where(n =>
                    n.nota.HasValue &&
                    string.Equals(
                        n.tipo_evaluacion,
                        tipo,
                        StringComparison.OrdinalIgnoreCase
                    ))
                .Select(n => n.nota.Value)
                .ToList();

            if (!calificaciones.Any())
            {
                return null;
            }

            return Math.Round(
                calificaciones.Average(),
                2
            );
        }

        // ========================================
        // CONSTRUCTOR
        // ========================================

        public ResumenNotasDto()
        {
            Notas = new List<NotaDto>();
        }
    }
}