using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Roles
{
    public class RolDto
    {
        public int id_rol { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre del rol no puede superar los 50 caracteres.")]
        [DisplayName("Nombre del rol")]
        public string nombre { get; set; }

        [DisplayName("Cantidad de usuarios")]
        public int cantidadUsuarios { get; set; }
    }
}
