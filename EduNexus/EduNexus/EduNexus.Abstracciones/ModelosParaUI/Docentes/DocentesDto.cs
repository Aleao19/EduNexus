using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Docentes
{
    public class DocentesDto
    {
        public int id_docente { get; set; }
        [Required]
        [DisplayName("Identificación")]
        public string identificacion { get; set; }
        [Required]
        [DisplayName("Nombre")]
        public string nombre { get; set; }
        [Required]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }
        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }
        [Required]
        [DisplayName("Email")]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string email { get; set; }
        [DisplayName("Teléfono")]
        public string telefono { get; set; }
        [DisplayName("Estado")]
        public string estado { get; set; }
    }
}