namespace EduNexus.UI.Models.Identity
{
    // Tabla: secciones
    public class SeccionEntity
    {
        public int id_seccion { get; set; }
        public string nombre { get; set; }
        public int cupo { get; set; }
        public int fk_id_grado { get; set; }
        public int fk_id_calendario { get; set; }
    }
}
