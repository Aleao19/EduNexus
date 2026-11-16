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
        // Periodo del calendario al que pertenece la sección (calendario.id_calendario)
        public int id_calendario { get; set; }
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
        // Distribución de los estudiantes asignados (género y necesidades)
        [DisplayName("Femenino")]
        public int estudiantes_femenino { get; set; }
        [DisplayName("Masculino")]
        public int estudiantes_masculino { get; set; }
        [DisplayName("Otro")]
        public int estudiantes_otro { get; set; }
        [DisplayName("Sin dato de género")]
        public int estudiantes_sin_genero { get; set; }
        [DisplayName("Con necesidades")]
        public int estudiantes_con_necesidades { get; set; }
        [DisplayName("Fecha de modificación")]
        public DateTime? fecha_modificacion { get; set; }
        [DisplayName("Modificado por")]
        public string usuario_modificacion { get; set; }

    }

    public class EstudianteSeccionVm
    {
        public string id_usuario { get; set; }
        public string nombreCompleto { get; set; }
        public string identificacion { get; set; }
        public DateTime fecha_asignacion { get; set; }
        // "F", "M", "Otro" o "Sin dato"
        public string genero { get; set; }
        public bool tiene_necesidades { get; set; }
        // Sección sugerida si se quiere mover (null si no hay destino con cupo)
        public int? id_seccion_sugerida { get; set; }
        public string seccion_sugerida { get; set; }
        public string motivo_sugerencia { get; set; }
    }

    // Estudiante que se puede asignar (combo de "Asignar estudiantes")
    public class EstudianteDisponibleVm
    {
        public string id_usuario { get; set; }
        public string texto { get; set; }
        public string genero { get; set; }
        public bool tiene_necesidades { get; set; }
        public int? id_seccion_sugerida { get; set; }
        public string seccion_sugerida { get; set; }
        public string motivo_sugerencia { get; set; }
    }

    // Composición de una sección del mismo grado y año lectivo
    public class DistribucionSeccionVm
    {
        public int id_seccion { get; set; }
        public string texto { get; set; }
        public int cupo { get; set; }
        public int asignados { get; set; }
        public int disponibles { get; set; }
        public int femenino { get; set; }
        public int masculino { get; set; }
        public int otro { get; set; }
        public int sin_genero { get; set; }
        public int con_necesidades { get; set; }
        public bool es_actual { get; set; }
    }

    public class SeccionDestinoVm
    {
        public int id_seccion { get; set; }
        public string texto { get; set; }
        public int disponibles { get; set; }
        // "F 10 · M 9 · Otro 0 · Necesidades 2"
        public string resumen { get; set; }
        public bool tiene_horario { get; set; }
    }
}
