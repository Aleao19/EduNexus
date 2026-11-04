using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Materias
{
    public class MateriaSeccionDto
{
    public int id_seccion_materia { get; set; }
    public int id_seccion { get; set; }
    public string seccion { get; set; }
    public int id_materia { get; set; }
    public string materia { get; set; }
    public string profesor { get; set; }
}

public class AsignarMateriaDto
{
    [Required(ErrorMessage = "Debe seleccionar una materia")]
    [DisplayName("Materia")]
    public int? id_materia { get; set; }

    [Required(ErrorMessage = "La sección es obligatoria")]
    [DisplayName("Sección")]
    public string seccion { get; set; }
}
}
