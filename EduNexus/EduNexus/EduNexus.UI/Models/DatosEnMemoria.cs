using EduNexus.Abstracciones.ModelosParaUI.Grados;
using EduNexus.Abstracciones.ModelosParaUI.Secciones;
using System.Collections.Generic;

namespace EduNexus.UI.Models
{
    // Datos de prueba compartidos entre controladores (Grados y Secciones)
    // para que los cambios se mantengan mientras la aplicación esté en ejecución.
    public static class DatosEnMemoria
    {
        public static readonly object Bloqueo = new object();

        public static List<GradosDto> Grados = new List<GradosDto>
        {
            new GradosDto { id_grado = 1, grado = 1, descripcion = "Primer Grado" },
            new GradosDto { id_grado = 2, grado = 2, descripcion = "Segundo Grado" },
            new GradosDto { id_grado = 3, grado = 3, descripcion = "Tercer Grado" },
            new GradosDto { id_grado = 4, grado = 4, descripcion = "Cuarto Grado" },
            new GradosDto { id_grado = 5, grado = 5, descripcion = "Quinto Grado" },
            new GradosDto { id_grado = 6, grado = 6, descripcion = "Sexto Grado" },
        };

        public static List<SeccionesDto> Secciones = new List<SeccionesDto>
        {
            new SeccionesDto { id_seccion = 1, nombre = "1-A", grado = 1, anio = 2026, cupo = 30, estudiantes_matriculados = 25 },
            new SeccionesDto { id_seccion = 2, nombre = "1-B", grado = 1, anio = 2026, cupo = 30, estudiantes_matriculados = 0 },
            new SeccionesDto { id_seccion = 3, nombre = "2-A", grado = 2, anio = 2026, cupo = 30, estudiantes_matriculados = 28 },
            new SeccionesDto { id_seccion = 4, nombre = "2-B", grado = 2, anio = 2026, cupo = 30, estudiantes_matriculados = 0 },
            new SeccionesDto { id_seccion = 5, nombre = "3-A", grado = 3, anio = 2026, cupo = 30, estudiantes_matriculados = 27 },
            new SeccionesDto { id_seccion = 6, nombre = "4-A", grado = 4, anio = 2026, cupo = 30, estudiantes_matriculados = 30 },
            new SeccionesDto { id_seccion = 7, nombre = "5-A", grado = 5, anio = 2026, cupo = 30, estudiantes_matriculados = 26 },
            new SeccionesDto { id_seccion = 9, nombre = "1-A", grado = 1, anio = 2025, cupo = 30, estudiantes_matriculados = 30 },
            new SeccionesDto { id_seccion = 10, nombre = "2-A", grado = 2, anio = 2025, cupo = 30, estudiantes_matriculados = 24 },
        };
    }
}
