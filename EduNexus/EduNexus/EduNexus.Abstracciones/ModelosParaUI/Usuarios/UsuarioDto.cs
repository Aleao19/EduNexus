using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Usuarios
{
    public class UsuarioDto
    {
        public int id_usuario { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [StringLength(20, ErrorMessage = "La cédula no puede superar los 20 caracteres.")]
        [DisplayName("Cédula")]
        public string cedula { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [DisplayName("Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }

        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [DisplayName("Correo electrónico")]
        public string correo { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol.")]
        [DisplayName("Rol")]
        public int id_rol { get; set; }

        [DisplayName("Rol")]
        public string nombreRol { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [DisplayName("Estado")]
        public string estado { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [DisplayName("Contraseña")]
        public string contrasenna { get; set; }
    }
}
