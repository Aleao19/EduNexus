using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: bitacora (registro de auditoría)
    public class BitacoraEntity
    {
        public int id_evento { get; set; }
        public string tipo_de_evento { get; set; }
        public string descripcion_evento { get; set; }
        public DateTime fecha { get; set; }
        public string datos_anteriores { get; set; }
        public string datos_posteriores { get; set; }
        public string fk_id_usuario { get; set; }
    }
}
