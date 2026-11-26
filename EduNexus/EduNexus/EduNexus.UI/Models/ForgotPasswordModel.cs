using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.UI.Models
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [DisplayName("Correo electrónico")]
        public string Email { get; set; }
    }
}
