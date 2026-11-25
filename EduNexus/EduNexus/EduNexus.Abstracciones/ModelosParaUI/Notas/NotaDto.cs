using System;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Notas
{
    public class NotaDto
    {
        // ========================================
        // IDENTIFICADOR DE LA NOTA
        // ========================================

        public int id_nota { get; set; }

        // ========================================
        // INFORMACIÓN DEL ESTUDIANTE
        // ========================================

        [Required(ErrorMessage = "Debe seleccionar un estudiante.")]
        [StringLength(128)]
        public string fk_id_estudiante { get; set; }

        [Display(Name = "Identificación del estudiante")]
        public string identificacion_estudiante { get; set; }

        [Display(Name = "Nombre del estudiante")]
        public string nombre_estudiante { get; set; }

        // ========================================
        // INFORMACIÓN DEL GRADO Y SECCIÓN
        // ========================================

        public int id_grado { get; set; }

        [Display(Name = "Grado")]
        public string nombre_grado { get; set; }

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
        // CALENDARIO Y PERÍODO ACADÉMICO
        // ========================================

        [Display(Name = "Calendario académico")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un calendario académico.")]
        public int fk_id_calendario { get; set; }

        public string nombre_calendario { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un período académico.")]
        [StringLength(50)]
        [Display(Name = "Período académico")]
        public string periodo { get; set; }

        // ========================================
        // INFORMACIÓN DE LA EVALUACIÓN
        // ========================================

        [Required(ErrorMessage = "Debe ingresar el nombre de la evaluación.")]
        [StringLength(150,
            ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre de la evaluación")]
        public string nombre_evaluacion { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el tipo de evaluación.")]
        [StringLength(50)]
        [Display(Name = "Tipo de evaluación")]
        public string tipo_evaluacion { get; set; }

        [Required(ErrorMessage = "Debe indicar la fecha de evaluación.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de evaluación")]
        public DateTime? fecha_evaluacion { get; set; }

        // ========================================
        // CALIFICACIÓN DEL ESTUDIANTE
        // ========================================

        [Required(ErrorMessage = "Debe ingresar una calificación.")]
        [Range(typeof(decimal), "0", "100",
            ErrorMessage = "La nota debe estar entre 0 y 100.")]
        [Display(Name = "Calificación")]
        public decimal? nota { get; set; }

        // ========================================
        // OBSERVACIONES
        // ========================================

        [StringLength(500,
            ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Observaciones")]
        public string observaciones { get; set; }

        // ========================================
        // CONTROL DEL REGISTRO
        // ========================================

        [Display(Name = "Fecha de registro")]
        public DateTime fecha_registro { get; set; }

        [Display(Name = "Fecha de modificación")]
        public DateTime? fecha_modificacion { get; set; }

        public string fk_id_modificado_por { get; set; }

        public string nombre_modificado_por { get; set; }

        [Display(Name = "Estado")]
        public string estado { get; set; }

        // ========================================
        // CONSTRUCTOR
        // ========================================

        public NotaDto()
        {
            fecha_evaluacion = DateTime.Today;

            estado = "Registrada";

            nota = null;
        }
    }
}