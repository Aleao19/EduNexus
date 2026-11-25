using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduNexus.UI.Models.Identity
{
    [Table("notas")]
    public class NotaEntity
    {
        // ========================================
        // IDENTIFICADOR DE LA NOTA
        // ========================================

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_nota { get; set; }

        // ========================================
        // INFORMACIÓN DEL ESTUDIANTE
        // ========================================

        [Required]
        [StringLength(128)]
        public string fk_id_estudiante { get; set; }

        [Required]
        public int fk_id_seccion { get; set; }

        [Required]
        public int fk_id_materia { get; set; }

        // ========================================
        // INFORMACIÓN DEL DOCENTE
        // ========================================

        [Required]
        [StringLength(128)]
        public string fk_id_docente { get; set; }

        // ========================================
        // CALENDARIO Y PERÍODO ACADÉMICO
        // ========================================

        [Required]
        public int fk_id_calendario { get; set; }

        [Required]
        [StringLength(50)]
        public string periodo { get; set; }

        // ========================================
        // INFORMACIÓN DE LA EVALUACIÓN
        // ========================================

        [Required]
        [StringLength(150)]
        public string nombre_evaluacion { get; set; }

        [Required]
        [StringLength(50)]
        public string tipo_evaluacion { get; set; }

        [Required]
        public DateTime fecha_evaluacion { get; set; }

        // ========================================
        // CALIFICACIÓN DEL ESTUDIANTE
        // ========================================

        [Required]
        [Range(typeof(decimal), "0", "100",
            ErrorMessage = "La nota debe estar entre 0 y 100.")]
        public decimal nota { get; set; }

        [StringLength(500)]
        public string observaciones { get; set; }

        // ========================================
        // CONTROL DEL REGISTRO
        // ========================================

        [Required]
        public DateTime fecha_registro { get; set; }

        public DateTime? fecha_modificacion { get; set; }

        [StringLength(128)]
        public string fk_id_modificado_por { get; set; }

        [Required]
        [StringLength(30)]
        public string estado { get; set; }
    }
}