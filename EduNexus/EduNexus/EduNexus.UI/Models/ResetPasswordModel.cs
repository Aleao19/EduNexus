using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.UI.Models
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        public string Token { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [DisplayName("Nueva Contraseña")]
        public string NuevaContrasenna { get; set; }

        [Required(ErrorMessage = "Confirmar la nueva contraseña es obligatorio.")]
        [DataType(DataType.Password)]
        [DisplayName("Confirmar Nueva Contraseña")]
        public string ConfirmarContrasenna { get; set; }
    }
}
