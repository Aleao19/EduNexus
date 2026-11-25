using System;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Notas
{
    public class ModificarNotaDto
    {
        // ========================================
        // IDENTIFICACIÓN DE LA CALIFICACIÓN
        // ========================================

        [Range(1, int.MaxValue,
            ErrorMessage = "La calificación seleccionada no es válida.")]
        public int id_nota { get; set; }

        // ========================================
        // INFORMACIÓN DEL ESTUDIANTE
        // ========================================

        public string fk_id_estudiante { get; set; }

        [Display(Name = "Identificación del estudiante")]
        public string identificacion_estudiante { get; set; }

        [Display(Name = "Nombre del estudiante")]
        public string nombre_estudiante { get; set; }

        // ========================================
        // INFORMACIÓN ACADÉMICA
        // ========================================

        public int id_grado { get; set; }

        [Display(Name = "Grado")]
        public string nombre_grado { get; set; }

        public int fk_id_seccion { get; set; }

        [Display(Name = "Sección")]
        public string nombre_seccion { get; set; }

        public int fk_id_materia { get; set; }

        [Display(Name = "Materia")]
        public string nombre_materia { get; set; }

        public string fk_id_docente { get; set; }

        [Display(Name = "Docente")]
        public string nombre_docente { get; set; }

        public int fk_id_calendario { get; set; }

        [Display(Name = "Calendario académico")]
        public string nombre_calendario { get; set; }

        [Display(Name = "Período académico")]
        public string periodo { get; set; }

        // ========================================
        // INFORMACIÓN DE LA EVALUACIÓN
        // ========================================

        [Display(Name = "Nombre de la evaluación")]
        public string nombre_evaluacion { get; set; }

        [Display(Name = "Tipo de evaluación")]
        public string tipo_evaluacion { get; set; }

        [Display(Name = "Fecha de evaluación")]
        [DataType(DataType.Date)]
        public DateTime? fecha_evaluacion { get; set; }

        // ========================================
        // INFORMACIÓN ORIGINAL
        // ========================================

        [Display(Name = "Calificación original")]
        public decimal nota_original { get; set; }

        [Display(Name = "Observaciones originales")]
        public string observaciones_originales { get; set; }

        // ========================================
        // CAMPOS QUE SE PUEDEN MODIFICAR
        // ========================================

        [Required(
            ErrorMessage = "Debe ingresar la nueva calificación.")]

        [Range(typeof(decimal), "0", "100",
            ErrorMessage = "La nota debe estar entre 0 y 100.")]

        [Display(Name = "Nueva calificación")]
        public decimal? nota { get; set; }

        [StringLength(500,
            ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]

        [DataType(DataType.MultilineText)]

        [Display(Name = "Observaciones")]
        public string observaciones { get; set; }

        // ========================================
        // MOTIVO DE LA MODIFICACIÓN
        // ========================================

        [Required(
            ErrorMessage = "Debe indicar el motivo de la modificación.")]

        [StringLength(500,
            MinimumLength = 5,
            ErrorMessage = "El motivo debe contener entre 5 y 500 caracteres.")]

        [DataType(DataType.MultilineText)]

        [Display(Name = "Motivo de la modificación")]
        public string motivo_modificacion { get; set; }

        // ========================================
        // INFORMACIÓN DEL REGISTRO
        // ========================================

        [Display(Name = "Fecha de registro")]
        public DateTime fecha_registro { get; set; }

        [Display(Name = "Última modificación")]
        public DateTime? fecha_modificacion { get; set; }

        public string fk_id_modificado_por { get; set; }

        [Display(Name = "Modificado por")]
        public string nombre_modificado_por { get; set; }

        [Display(Name = "Estado")]
        public string estado { get; set; }

        // ========================================
        // CONTROL DE CONCURRENCIA
        // ========================================

        public string version_registro { get; set; }

        // ========================================
        // CONFIRMACIÓN DE LA MODIFICACIÓN
        // ========================================

        [Display(Name = "Confirmo que los cambios realizados son correctos.")]
        public bool confirma_modificacion { get; set; }

        // ========================================
        // CONSTRUCTOR
        // ========================================

        public ModificarNotaDto()
        {
            nota = null;
            confirma_modificacion = false;
        }
    }
}