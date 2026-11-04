namespace EduNexus.UI.Models.Identity
{
    // Tabla: asignaciones (evaluaciones y calificaciones del estudiante)
    public class AsignacionEntity
    {
        public int id_asignacion { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal porcentaje_nota { get; set; }
        public decimal calificacion { get; set; }
        public string estado { get; set; }
        public string fk_id_usuario { get; set; }
        public int fk_id_seccion { get; set; }
        public int? fk_id_materia { get; set; }
    }
}
