using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: calendario (periodo o año lectivo)
    public class CalendarioEntity
    {
        public int id_calendario { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime fecha_fin { get; set; }
    }
}
