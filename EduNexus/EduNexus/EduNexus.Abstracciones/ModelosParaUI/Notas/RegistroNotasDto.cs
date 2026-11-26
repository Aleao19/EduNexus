using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EduNexus.Abstracciones.ModelosParaUI.Notas
{
    public class RegistroNotasDto
    {
        // ========================================
        // INFORMACIÓN DEL GRADO
        // ========================================

        [Display(Name = "Grado")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un grado.")]
        public int id_grado { get; set; }

        public string nombre_grado { get; set; }

        // ========================================
        // INFORMACIÓN DE LA SECCIÓN
        // ========================================

        [Display(Name = "Sección")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar una sección.")]
        public int fk_id_seccion { get; set; }

        public string nombre_seccion { get; set; }

        // ========================================
        // INFORMACIÓN DE LA MATERIA
        // ========================================

        [Display(Name = "Materia")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar una materia.")]
        public int fk_id_materia { get; set; }

        public string nombre_materia { get; set; }

        // ========================================
        // INFORMACIÓN DEL DOCENTE
        // ========================================

        public string fk_id_docente { get; set; }

        [Display(Name = "Docente")]
        public string nombre_docente { get; set; }

        // ========================================
        // CALENDARIO ACADÉMICO
        // ========================================

        [Display(Name = "Calendario académico")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un calendario académico.")]
        public int fk_id_calendario { get; set; }

        public string nombre_calendario { get; set; }

        // ========================================
        // PERÍODO ACADÉMICO
        // ========================================

        [Required(ErrorMessage =
            "Debe seleccionar un período académico.")]
        [StringLength(50)]
        [Display(Name = "Período académico")]
        public string periodo { get; set; }

        // ========================================
        // INFORMACIÓN DE LA EVALUACIÓN
        // ========================================

        [Required(ErrorMessage =
            "Debe ingresar el nombre de la evaluación.")]
        [StringLength(150,
            ErrorMessage =
            "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre de la evaluación")]
        public string nombre_evaluacion { get; set; }

        [Required(ErrorMessage =
            "Debe seleccionar el tipo de evaluación.")]
        [StringLength(50)]
        [Display(Name = "Tipo de evaluación")]
        public string tipo_evaluacion { get; set; }

        [Required(ErrorMessage =
            "Debe indicar la fecha de evaluación.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de evaluación")]
        public DateTime? fecha_evaluacion { get; set; }

        // ========================================
        // LISTADO DE ESTUDIANTES Y CALIFICACIONES
        // ========================================

        public List<NotaEstudianteDto> Estudiantes { get; set; }

        // ========================================
        // ESTADÍSTICAS DEL REGISTRO
        // ========================================

        public int TotalEstudiantes
        {
            get
            {
                return Estudiantes == null
                    ? 0
                    : Estudiantes.Count;
            }
        }

        public int TotalCalificados
        {
            get
            {
                return Estudiantes == null
                    ? 0
                    : Estudiantes.Count(e =>
                        e != null &&
                        e.nota.HasValue &&
                        e.nota.Value >= 0 &&
                        e.nota.Value <= 100);
            }
        }

        public int TotalPendientes
        {
            get
            {
                return TotalEstudiantes - TotalCalificados;
            }
        }

        public decimal PromedioGrupo
        {
            get
            {
                if (Estudiantes == null)
                {
                    return 0;
                }

                var notasValidas = Estudiantes
                    .Where(e =>
                        e != null &&
                        e.nota.HasValue &&
                        e.nota.Value >= 0 &&
                        e.nota.Value <= 100)
                    .Select(e => e.nota.Value)
                    .ToList();

                if (!notasValidas.Any())
                {
                    return 0;
                }

                return Math.Round(
                    notasValidas.Average(), 2);
            }
        }

        public decimal NotaMayor
        {
            get
            {
                if (Estudiantes == null)
                {
                    return 0;
                }

                var notasValidas = Estudiantes
                    .Where(e =>
                        e != null &&
                        e.nota.HasValue &&
                        e.nota.Value >= 0 &&
                        e.nota.Value <= 100)
                    .Select(e => e.nota.Value)
                    .ToList();

                return notasValidas.Any()
                    ? notasValidas.Max()
                    : 0;
            }
        }

        public decimal NotaMenor
        {
            get
            {
                if (Estudiantes == null)
                {
                    return 0;
                }

                var notasValidas = Estudiantes
                    .Where(e =>
                        e != null &&
                        e.nota.HasValue &&
                        e.nota.Value >= 0 &&
                        e.nota.Value <= 100)
                    .Select(e => e.nota.Value)
                    .ToList();

                return notasValidas.Any()
                    ? notasValidas.Min()
                    : 0;
            }
        }

        // ========================================
        // CONFIRMACIÓN DEL REGISTRO
        // ========================================

        [Display(Name =
            "Confirmo que las calificaciones ingresadas son correctas.")]
        public bool confirma_registro { get; set; }

        // ========================================
        // CONSTRUCTOR
        // ========================================

        public RegistroNotasDto()
        {
            Estudiantes = new List<NotaEstudianteDto>();

            fecha_evaluacion = DateTime.Today;

            confirma_registro = false;
        }
    }

    // ============================================
    // MODELO INDIVIDUAL DE CALIFICACIÓN
    // ============================================

    public class NotaEstudianteDto
    {
        [Required(ErrorMessage =
            "No se ha identificado al estudiante.")]
        [StringLength(128)]
        public string fk_id_estudiante { get; set; }

        [Display(Name = "Identificación")]
        public string identificacion_estudiante { get; set; }

        [Display(Name = "Nombre del estudiante")]
        public string nombre_estudiante { get; set; }

        [Display(Name = "Calificación")]
        [Range(typeof(decimal), "0", "100",
            ErrorMessage =
            "La nota debe estar entre 0 y 100.")]
        public decimal? nota { get; set; }

        [Display(Name = "Observaciones")]
        [StringLength(500,
            ErrorMessage =
            "Las observaciones no pueden superar los 500 caracteres.")]
        public string observaciones { get; set; }

        public NotaEstudianteDto()
        {
            nota = null;
        }
    }
}