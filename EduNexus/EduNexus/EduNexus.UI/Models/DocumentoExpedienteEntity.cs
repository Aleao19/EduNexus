using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: documentos_expediente (REXP-03-002)
    // El archivo físico se guarda en App_Data/DocumentosExpedientes/{id_expediente}/{nombre_interno}
    public class DocumentoExpedienteEntity
    {
        public int id_documento { get; set; }
        public int fk_id_expediente { get; set; }
        public string nombre_documento { get; set; }
        public string descripcion { get; set; }
        public string nombre_archivo { get; set; }
        public string nombre_interno { get; set; }
        public string tipo_archivo { get; set; }
        public int tamano_bytes { get; set; }
        public DateTime fecha_registro { get; set; }
        public string fk_id_usuario { get; set; }
    }
}
