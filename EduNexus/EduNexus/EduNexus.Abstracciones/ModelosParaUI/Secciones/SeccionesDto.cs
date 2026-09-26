using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Secciones
{
    public class SeccionesDto
    {
        public  int id_seccion { get; set; }
        [Required(ErrorMessage = "El nombre de la sección es obligatorio")]
        [DisplayName("Nombre de la sección")]
        public string nombre { get; set; }
        // Id del grado al que pertenece la sección (id_grado de GradosDto)
        [DisplayName("Grado")]
        public int grado { get; set; }
        [Required(ErrorMessage = "El nombre del grado es obligatorio")]
        [DisplayName("Nombre del grado")]
        public string nombre_grado { get; set; }
        [Required(ErrorMessage = "El año lectivo es obligatorio")]
        [DisplayName("Año lectivo")]
        public int? anio { get; set; }
        [Required]
        [Range(1, 35, ErrorMessage= "El cupo debe ser entre 1 y 35")]
        [DisplayName("Cupo")]
        public int cupo { get; set; }
        [DisplayName("Estudiantes matriculados")]
        public int estudiantes_matriculados { get; set; }
        // RGRU-06-003: cupos disponibles = cupo - estudiantes asignados
        [DisplayName("Cupos disponibles")]
        public int cupos_disponibles
        {
            get { return Math.Max(0, cupo - estudiantes_matriculados); }
        }
        [DisplayName("Fecha de modificación")]
        public DateTime? fecha_modificacion { get; set; }
        [DisplayName("Modificado por")]
        public string usuario_modificacion { get; set; }
    }
}
