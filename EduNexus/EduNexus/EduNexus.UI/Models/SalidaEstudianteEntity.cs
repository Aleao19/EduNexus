using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduNexus.UI.Models.Identity
{
    // RJT-11-002: Registrar salida de estudiantes.

    [Table("salidas_estudiantes")]
    public class SalidaEstudianteEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_salida { get; set; }

        [Required]
        [StringLength(128)]
        public string fk_id_estudiante { get; set; }

        [Required]
        public int fk_id_seccion { get; set; }

        [Required]
        public DateTime fecha_salida { get; set; }

        [Required]
        [StringLength(500)]
        public string motivo { get; set; }

        [Required]
        [StringLength(256)]
        public string nombre_persona_retira { get; set; }

        [Required]
        [StringLength(50)]
        public string identificacion_persona_retira { get; set; }

        [Required]
        [StringLength(100)]
        public string parentesco { get; set; }

        [StringLength(30)]
        public string telefono_persona_retira { get; set; }

        [Required]
        [StringLength(128)]
        public string fk_id_registrado_por { get; set; }

        [Required]
        public DateTime fecha_registro { get; set; }

        [StringLength(500)]
        public string observaciones { get; set; }

        [Required]
        [StringLength(30)]
        public string estado { get; set; }
    }
}