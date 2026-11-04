
using System;

namespace EduNexus.UI.Models.Identity
{
    public class ExpedienteEntity
    {
        public int id_expediente { get; set; }
        public int fk_id_prematricula { get; set; }
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
    }
}
