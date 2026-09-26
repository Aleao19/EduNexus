using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Grados
{
    public class GradosDto
    {
        public int id_grado { get; set; }

        [Required]
        [Range(1, 6, ErrorMessage = "El grado debe ser entre 1 y 6")]
        [DisplayName("Grado")]
        public int grado { get; set; }

        [Required]
        [DisplayName("Descripción")]
        public string descripcion { get; set; }
    }
}