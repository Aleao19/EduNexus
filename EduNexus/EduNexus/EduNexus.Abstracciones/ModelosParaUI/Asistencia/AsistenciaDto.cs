using System;
using System.Collections.Generic;

namespace EduNexus.Abstracciones.ModelosParaUI.Asistencia
{
    public class AsistenciaEstudianteDto
    {
        public string id_usuario { get; set; }
        public string nombre { get; set; }
        public string identificacion { get; set; }
        public string estado { get; set; }
        public bool sinMarcar { get; set; }

        // Trazabilidad de la justificación (solo para "Ausencia justificada")
        public string motivo_justificacion { get; set; }
        public string justificado_por { get; set; }
        public DateTime? fecha_justificacion { get; set; }
        public string errorMotivo { get; set; }
    }

    public class AsistenciaFormDto
    {
        public string clase { get; set; }    
        public DateTime? fecha { get; set; }
        public bool esEdicion { get; set; }
        public string titulo { get; set; }
        public bool yaRegistrado { get; set; }
        public bool sinClases { get; set; }
        public string mensajeError { get; set; }
        public static readonly string[] Estados =
            { "Presente", "Tardía", "Ausencia no justificada", "Ausencia justificada" };

        // Estado que exige motivo de justificación
        public const string EstadoJustificado = "Ausencia justificada";
        public const int MaximoMotivo = 500;

        public bool modoAjax { get; set; }
        public string urlGuardar { get; set; }
        public string urlEditar { get; set; }
        public string urlCancelar { get; set; }
        public string urlHistorial { get; set; }
        public string mensajeExito { get; set; }
        public List<AsistenciaEstudianteDto> estudiantes { get; set; } = new List<AsistenciaEstudianteDto>();
    }

    // Historial de asistencia de una clase por rango de fechas
    public class AsistenciaHistorialDto
    {
        public string clase { get; set; }
        public DateTime? desde { get; set; }
        public DateTime? hasta { get; set; }
        public string titulo { get; set; }
        public bool sinClases { get; set; }
        public string mensajeError { get; set; }
        public bool consultado { get; set; }
        public List<DateTime> fechas { get; set; } = new List<DateTime>();
        public List<AsistenciaHistorialEstudianteDto> estudiantes { get; set; } = new List<AsistenciaHistorialEstudianteDto>();
        public List<AsistenciaJustificacionDto> justificaciones { get; set; } = new List<AsistenciaJustificacionDto>();
    }

    public class AsistenciaHistorialEstudianteDto
    {
        public string id_usuario { get; set; }
        public string nombre { get; set; }
        public string identificacion { get; set; }
        // Clave: fecha en formato yyyy-MM-dd; valor: estado registrado
        public Dictionary<string, string> estados { get; set; } = new Dictionary<string, string>();
        public int presentes { get; set; }
        public int tardias { get; set; }
        public int ausenciasJustificadas { get; set; }
        public int ausenciasNoJustificadas { get; set; }
    }

    public class AsistenciaJustificacionDto
    {
        public DateTime fecha { get; set; }
        public string estudiante { get; set; }
        public string motivo { get; set; }
        public string justificado_por { get; set; }
        public DateTime? fecha_justificacion { get; set; }
    }
}
