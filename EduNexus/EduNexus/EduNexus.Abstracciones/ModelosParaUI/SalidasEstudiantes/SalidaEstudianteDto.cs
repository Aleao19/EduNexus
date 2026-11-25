using System;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.SalidasEstudiantes
{
    public class SalidaEstudianteDto
    {
        public int id_salida { get; set; }

        // ========================================
        // INFORMACIÓN DEL ESTUDIANTE
        // ========================================

        [Required(ErrorMessage = "Debe seleccionar un estudiante.")]
        [Display(Name = "Estudiante")]
        [StringLength(128)]
        public string fk_id_estudiante { get; set; }

        [Display(Name = "Identificación del estudiante")]
        public string identificacion_estudiante { get; set; }

        [Display(Name = "Nombre del estudiante")]
        public string nombre_estudiante { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una sección.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar una sección válida.")]
        [Display(Name = "Sección")]
        public int fk_id_seccion { get; set; }

        [Display(Name = "Nombre de la sección")]
        public string nombre_seccion { get; set; }

        [Display(Name = "Grado")]
        public int id_grado { get; set; }

        [Display(Name = "Nombre del grado")]
        public string nombre_grado { get; set; }

        // ========================================
        // INFORMACIÓN DE LA SALIDA
        // ========================================

        [Required(ErrorMessage = "Debe indicar la fecha de salida.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de salida")]
        public DateTime? fecha_salida { get; set; }

        [Required(ErrorMessage = "Debe indicar la hora de salida.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de salida")]
        public TimeSpan? hora_salida { get; set; }

        [Required(ErrorMessage = "Debe indicar el motivo de salida.")]
        [StringLength(500,
            ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Motivo de salida")]
        public string motivo { get; set; }

        // ========================================
        // PERSONA QUE RETIRA AL ESTUDIANTE
        // ========================================

        [Required(ErrorMessage = "Debe indicar el nombre de la persona que retira al estudiante.")]
        [StringLength(256,
            ErrorMessage = "El nombre no puede superar los 256 caracteres.")]
        [Display(Name = "Nombre de la persona que retira")]
        public string nombre_persona_retira { get; set; }

        [Required(ErrorMessage = "Debe indicar la identificación de la persona que retira.")]
        [StringLength(50,
            ErrorMessage = "La identificación no puede superar los 50 caracteres.")]
        [Display(Name = "Identificación de la persona que retira")]
        public string identificacion_persona_retira { get; set; }

        [Required(ErrorMessage = "Debe indicar el parentesco con el estudiante.")]
        [StringLength(100,
            ErrorMessage = "El parentesco no puede superar los 100 caracteres.")]
        [Display(Name = "Parentesco")]
        public string parentesco { get; set; }

        [StringLength(30,
            ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Teléfono de contacto")]
        public string telefono_persona_retira { get; set; }

        // ========================================
        // INFORMACIÓN DEL RESPONSABLE DEL REGISTRO
        // ========================================

        [Display(Name = "Usuario responsable")]
        public string fk_id_registrado_por { get; set; }

        [Display(Name = "Registrado por")]
        public string nombre_registrado_por { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime fecha_registro { get; set; }

        // ========================================
        // INFORMACIÓN ADICIONAL
        // ========================================

        [StringLength(500,
            ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Observaciones")]
        public string observaciones { get; set; }

        [Display(Name = "Estado")]
        public string estado { get; set; }

        // ========================================
        // CONFIRMACIÓN DEL RETIRO
        // ========================================

        [Display(Name = "Confirmo que se verificó la identidad y autorización de la persona que retira al estudiante.")]
        public bool confirma_retiro { get; set; }

        // ========================================
        // CONSTRUCTOR
        // ========================================

        public SalidaEstudianteDto()
        {
            fecha_salida = DateTime.Today;

            hora_salida = new TimeSpan(
                DateTime.Now.Hour,
                DateTime.Now.Minute,
                0);

            estado = "Registrada";

            confirma_retiro = false;
        }
    }
}