using System;

namespace EduNexus.UI.Models.Identity
{
    // Tabla: material_clase
    public class MaterialClaseEntity
    {
        public int id_material { get; set; }
        public int fk_id_seccion { get; set; }
        public int fk_id_materia { get; set; }
        public string nombre { get; set; }
        public string tipo { get; set; }
        public string url { get; set; }
        public string nombre_archivo { get; set; }
        public string nombre_interno { get; set; }
        public DateTime fecha_publicacion { get; set; }
        public string fk_id_publicado_por { get; set; }
    }
}