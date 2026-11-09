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
        public bool modoAjax { get; set; }
        public string urlGuardar { get; set; }
        public string urlEditar { get; set; }
        public string urlCancelar { get; set; }
        public string mensajeExito { get; set; }
        public List<AsistenciaEstudianteDto> estudiantes { get; set; } = new List<AsistenciaEstudianteDto>();
    }
}