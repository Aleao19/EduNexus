using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.ExpedienteAcademico
{
    // Estudiante (usuario con rol Estudiante) para el listado y la ficha del expediente
    public class EstudianteResumenDto
    {
        public string id_usuario { get; set; }
        [DisplayName("Identificación")]
        public string identificacion { get; set; }
        [DisplayName("Nombre")]
        public string nombre_completo { get; set; }
        [DisplayName("Correo")]
        public string correo { get; set; }
        [DisplayName("Estado")]
        public string estado { get; set; }
        [DisplayName("Sección actual")]
        public string seccion_actual { get; set; }
        [DisplayName("Adecuaciones activas")]
        public int adecuaciones_activas { get; set; }
        [DisplayName("Ayudas activas")]
        public int ayudas_activas { get; set; }
    }

    // REXP-02-007: un año lectivo del historial académico
    public class HistorialAnioDto
    {
        public int anio { get; set; }
        public string periodo { get; set; }
        public string grado { get; set; }
        public string seccion { get; set; }
        public decimal? promedio { get; set; }
        public List<CalificacionDto> calificaciones { get; set; } = new List<CalificacionDto>();
    }

    public class CalificacionDto
    {
        public string materia { get; set; }
        public string evaluacion { get; set; }
        public decimal porcentaje { get; set; }
        public decimal calificacion { get; set; }
        public string estado { get; set; }
    }

    // REXP-02-008: tipo de adecuación (catálogo)
    public class TipoAdecuacionDto
    {
        public int id_adecuacion { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de adecuación es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [DisplayName("Tipo de adecuación")]
        public string nombre { get; set; }

        [StringLength(512, ErrorMessage = "La descripción no puede superar los 512 caracteres.")]
        [DisplayName("Descripción")]
        public string descripcion { get; set; }

        [DisplayName("Casos activos")]
        public int casos_activos { get; set; }
    }

    // Caso de adecuación (REXP-02-008) o de ayuda (REXP-02-009) de un estudiante
    public class CasoApoyoDto
    {
        public int id_caso { get; set; }
        public string id_usuario { get; set; }

        [DisplayName("Estudiante")]
        public string estudiante { get; set; }

        [DisplayName("Tipo")]
        public int? id_tipo { get; set; }

        [DisplayName("Tipo")]
        public string tipo { get; set; }

        [Required(ErrorMessage = "La descripción del caso es obligatoria.")]
        [StringLength(512, ErrorMessage = "La descripción no puede superar los 512 caracteres.")]
        [DisplayName("Descripción del caso")]
        public string descripcion { get; set; }

        [DisplayName("Estado")]
        public string estado { get; set; }

        [DisplayName("Fecha de registro")]
        public DateTime fecha_registro { get; set; }
    }

    // Registro de un cambio de clasificación (tomado de la bitácora)
    public class CambioClasificacionDto
    {
        public DateTime fecha { get; set; }
        public string usuario { get; set; }
        public string tipo_anterior { get; set; }
        public string tipo_nuevo { get; set; }
    }
}
