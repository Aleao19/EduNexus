
using System;

namespace EduNexus.UI.Models.Identity
{
    public class PrematriculaEntity
    {
        public int id_prematricula { get; set; }

        public string identificacion { get; set; }

        public string nombre { get; set; }

        public string apellido1 { get; set; }

        public string apellido2 { get; set; }

        public string genero { get; set; }

        public int fk_id_grado { get; set; }

        public string telefono { get; set; }

        public DateTime fecha_nacimiento { get; set; }

        public bool tiene_adecuacion { get; set; }

        public bool tiene_discapacidad { get; set; }

        public string contactoEmergencia { get; set; }

        public string direccion { get; set; }

        public bool es_nuevo_ingreso { get; set; }

        public DateTime fecha_prematricula { get; set; }

        // RMPR-08-002: Estado de la solicitud
        public string estado { get; set; }

        // RMPR-08-007:
        // Periodo lectivo al que corresponde la solicitud.
        // Nullable para permitir una transición
        // desde los registros existentes.
        public int? fk_id_calendario { get; set; }
    }
}
