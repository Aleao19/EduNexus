using System;
using System.Collections.Generic;
using EduNexus.Abstracciones.ModelosParaUI.Bitacora;

namespace EduNexus.Abstracciones.ModelosParaUI.Dashboard
{

    public class DashboardViewModel
    {
        // ---- KPIs (tarjetas superiores) ----
        public int TotalEstudiantes { get; set; }
        public int TotalDocentes { get; set; }
        public int TotalSecciones { get; set; }
        public int TotalMaterias { get; set; }

        // Variación porcentual respecto al periodo anterior (solo visual, mock)
        public double VariacionEstudiantes { get; set; }
        public double VariacionDocentes { get; set; }
        public double VariacionSecciones { get; set; }
        public double VariacionMaterias { get; set; }


        // ---- Gráfica: matrícula histórica por año (la matrícula en Costa Rica
        //      es un proceso anual, no mensual) ----
        public List<string> AniosMatricula { get; set; } = new List<string>();
        public List<int> MatriculaPorAnio { get; set; } = new List<int>();

        // ---- Gráfica: estudiantes por sección (dona) ----
        public List<string> NombresSecciones { get; set; } = new List<string>();
        public List<int> EstudiantesPorSeccion { get; set; } = new List<int>();

        // ---- Gráfica: distribución de docentes por materia (barras) ----
        public List<string> NombresMaterias { get; set; } = new List<string>();
        public List<int> DocentesPorMateria { get; set; } = new List<int>();

        // ---- Actividad reciente (reutiliza el DTO de Bitácora existente) ----
        public List<BitacoraDto> ActividadReciente { get; set; } = new List<BitacoraDto>();

        // ---- Próximas clases / recordatorios rápidos ----
        public List<ProximaClaseDto> ProximasClases { get; set; } = new List<ProximaClaseDto>();
    }

    public class ProximaClaseDto
    {
        public string Materia { get; set; }
        public string Docente { get; set; }
        public string Seccion { get; set; }
        public string Hora { get; set; }
        public DayOfWeek Dia { get; set; }
    }
}