using System;

namespace EduNexus.Abstracciones.ModelosParaUI.Secciones
{
    // RGRU-06-002: estudiante asignado a una sección (pantalla "Asignar estudiantes").
    // Debe ser una clase pública: las vistas no pueden leer tipos anónimos del controlador.
    public class EstudianteAsignadoDto
    {
        public string nombreCompleto { get; set; }
        public string identificacion { get; set; }
        public DateTime fecha_asignacion { get; set; }
    }
}
