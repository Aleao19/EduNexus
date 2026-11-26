using System;

namespace EduNexus.Abstracciones.ModelosParaUI.Materiales
{
    public class MaterialDto
    {
        public int id_material { get; set; }
        public string nombre { get; set; }
        public string tipo { get; set; }              // "Archivo" o "Enlace"
        public string url { get; set; }
        public string nombre_archivo { get; set; }
        public DateTime fecha_publicacion { get; set; }
        public string publicado_por { get; set; }
    }
}