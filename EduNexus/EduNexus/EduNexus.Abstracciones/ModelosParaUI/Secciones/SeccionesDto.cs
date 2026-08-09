using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Secciones
{
    public class SeccionesDto
    {
        public  int id_seccion { get; set; }
        [Required]
        [DisplayName("Nombre de la sección")]
        public string nombre { get; set; }
        [Required]
        [Range(1, 6, ErrorMessage="El grado debe ser entre 1 y 6")]
        [DisplayName("Grado")]
        public int grado { get; set; }
        [Required]
        [Range(1, 35, ErrorMessage= "El cupo debe ser entre 1 y 35")]
        [DisplayName("Cupo")]
        public int cupo { get; set; }
    }
}
