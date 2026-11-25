using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EduNexus.Abstracciones.ModelosParaUI.Notas
{
    public class ConsultaNotasDto
    {
        // ========================================
        // INFORMACIÓN DEL USUARIO
        // ========================================

        public string id_usuario { get; set; }

        public string nombre_usuario { get; set; }

        public string rol_usuario { get; set; }

        // ========================================
        // INFORMACIÓN DEL ESTUDIANTE
        // ========================================

        public string id_estudiante { get; set; }

        public string identificacion_estudiante { get; set; }

        public string nombre_estudiante { get; set; }

        // ========================================
        // FILTROS DE CONSULTA
        // ========================================

        [Display(Name = "Grado")]
        public int? id_grado { get; set; }

        [Display(Name = "Sección")]
        public int? id_seccion { get; set; }

        [Display(Name = "Materia")]
        public int? id_materia { get; set; }

        [Display(Name = "Calendario académico")]
        public int? id_calendario { get; set; }

        [Display(Name = "Período académico")]
        public string periodo { get; set; }

        [Display(Name = "Tipo de evaluación")]
        public string tipo_evaluacion { get; set; }

        [Display(Name = "Fecha desde")]
        [DataType(DataType.Date)]
        public DateTime? fecha_desde { get; set; }

        [Display(Name = "Fecha hasta")]
        [DataType(DataType.Date)]
        public DateTime? fecha_hasta { get; set; }

        // ========================================
        // PERMISOS DE CONSULTA
        // ========================================

        public bool es_administrador { get; set; }

        public bool es_docente { get; set; }

        public bool es_estudiante { get; set; }

        public bool es_encargado { get; set; }

        public bool puede_seleccionar_estudiante { get; set; }

        // ========================================
        // LISTADO DE CALIFICACIONES
        // ========================================

        public List<NotaDto> Notas { get; set; }

        // ========================================
        // ESTADÍSTICAS GENERALES
        // ========================================

        public int TotalNotas
        {
            get
            {
                return Notas == null
                    ? 0
                    : Notas.Count(n => n.nota.HasValue);
            }
        }

        public int TotalMaterias
        {
            get
            {
                return Notas == null
                    ? 0
                    : Notas
                        .Select(n => n.fk_id_materia)
                        .Distinct()
                        .Count();
            }
        }

        public int TotalEstudiantes
        {
            get
            {
                return Notas == null
                    ? 0
                    : Notas
                        .Select(n => n.fk_id_estudiante)
                        .Distinct()
                        .Count();
            }
        }

        public decimal PromedioGeneral
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

        public bool TieneNotas
        {
            get
            {
                return Notas != null && Notas.Any();
            }
        }

        // ========================================
        // CONSTRUCTOR
        // ========================================

        public ConsultaNotasDto()
        {
            Notas = new List<NotaDto>();

            es_administrador = false;

            es_docente = false;

            es_estudiante = false;

            es_encargado = false;

            puede_seleccionar_estudiante = false;
        }
    }
}