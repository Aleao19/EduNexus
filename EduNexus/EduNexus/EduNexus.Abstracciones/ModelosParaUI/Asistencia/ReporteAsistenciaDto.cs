using System;
using System.Collections.Generic;

namespace EduNexus.Abstracciones.ModelosParaUI.Asistencia
{
    // RASI-10-005
    // Reporte mensual de asistencia.

    public class ReporteAsistenciaDto
    {
        public int anio { get; set; }

        public int mes { get; set; }

        public int? id_grado { get; set; }

        public int? id_seccion { get; set; }

        public string nombre_grado { get; set; }

        public string nombre_seccion { get; set; }

        public string nombre_mes { get; set; }

        public DateTime fecha_inicio { get; set; }

        public DateTime fecha_fin { get; set; }

        public bool consultado { get; set; }

        public string mensajeError { get; set; }

        public int total_estudiantes { get; set; }

        public int total_presentes { get; set; }

        public int total_tardias { get; set; }

        public int total_ausencias_justificadas { get; set; }

        public int total_ausencias_no_justificadas { get; set; }

        public int total_registros
        {
            get
            {
                return total_presentes +
                       total_tardias +
                       total_ausencias_justificadas +
                       total_ausencias_no_justificadas;
            }
        }

        public decimal porcentaje_asistencia
        {
            get
            {
                if (total_registros == 0)
                {
                    return 0;
                }

                return Math.Round(
                    (decimal)(total_presentes +
                              total_tardias)
                    * 100 / total_registros,
                    2);
            }
        }

        public decimal porcentaje_ausencias
        {
            get
            {
                if (total_registros == 0)
                {
                    return 0;
                }

                return Math.Round(
                    (decimal)(
                        total_ausencias_justificadas +
                        total_ausencias_no_justificadas)
                    * 100 / total_registros,
                    2);
            }
        }

        public List<ReporteAsistenciaEstudianteDto>
            estudiantes { get; set; }

        public ReporteAsistenciaDto()
        {
            anio = DateTime.Today.Year;

            mes = DateTime.Today.Month;

            estudiantes =
                new List<ReporteAsistenciaEstudianteDto>();
        }
    }

    // Detalle mensual de asistencia por estudiante.

    public class ReporteAsistenciaEstudianteDto
    {
        public string id_usuario { get; set; }

        public string identificacion { get; set; }

        public string nombre { get; set; }

        public int presentes { get; set; }

        public int tardias { get; set; }

        public int ausencias_justificadas { get; set; }

        public int ausencias_no_justificadas { get; set; }

        public int total_ausencias
        {
            get
            {
                return ausencias_justificadas +
                       ausencias_no_justificadas;
            }
        }

        public int total_registros
        {
            get
            {
                return presentes +
                       tardias +
                       ausencias_justificadas +
                       ausencias_no_justificadas;
            }
        }

        public decimal porcentaje_asistencia
        {
            get
            {
                if (total_registros == 0)
                {
                    return 0;
                }

                return Math.Round(
                    (decimal)(presentes + tardias)
                    * 100 / total_registros,
                    2);
            }
        }

        public decimal porcentaje_ausencias
        {
            get
            {
                if (total_registros == 0)
                {
                    return 0;
                }

                return Math.Round(
                    (decimal)total_ausencias
                    * 100 / total_registros,
                    2);
            }
        }
    }
}