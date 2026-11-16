using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Docentes
{
    public class DocentesDto
    {
        public string id_docente { get; set; }
        // Cédula con formato 0-0000-0000; solo se captura al registrar (después no se modifica)
        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [StringLength(20, ErrorMessage = "La cédula no puede superar los 20 caracteres.")]
        [DisplayName("Cédula")]
        public string identificacion { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [DisplayName("Nombre")]
        public string nombre { get; set; }
        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }
        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [DisplayName("Correo electrónico")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string email { get; set; }
        [DisplayName("Teléfono")]
        public string telefono { get; set; }
        [DisplayName("Estado")]
        public string estado { get; set; }
        // Solo al registrar: contraseña temporal para el primer ingreso (se guarda cifrada, nunca en texto plano)
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña temporal debe tener al menos 8 caracteres.")]
        [DisplayName("Contraseña temporal")]
        public string contrasenna_temporal { get; set; }
    }
}
