using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Materias
{
    public class MateriasDto
    {
        public int id_materia { get; set; }
        [Required(ErrorMessage = "El nombre de la materia es obligatorio")]
        [DisplayName("Nombre de la materia")]
        public string nombre { get; set; }
        [DisplayName("Profesor")]
        public string profesor { get; set; }
        [DisplayName("ID Profesor")]
        public string id_profesor { get; set; }
        [Display(Name = "Descripción")]
        [StringLength(100, ErrorMessage = "La descripción no puede superar los 100 caracteres.")]
        public string descripcion { get; set; }
    }
}
