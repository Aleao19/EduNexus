using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.UI.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [DisplayName("Correo electrónico")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [DisplayName("Contraseña")]
        public string Contrasenna { get; set; }

        public bool RememberMe { get; set; }
    }
}
