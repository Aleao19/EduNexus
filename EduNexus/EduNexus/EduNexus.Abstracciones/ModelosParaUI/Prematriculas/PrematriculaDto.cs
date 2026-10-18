using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Prematriculas
{
    public class PrematriculaDto
    {
        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [DisplayName("Identificación")]
        public string identificacion { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [DisplayName("Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }

        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }

        [DisplayName("Género")]
        public string genero { get; set; }

        [Required(ErrorMessage = "Seleccione el grado solicitado.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un grado válido.")]
        [DisplayName("Grado solicitado")]
        public int grado { get; set; }

        [DisplayName("Teléfono")]
        public string telefono { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayName("Fecha de nacimiento")]
        public DateTime? fecha_nacimiento { get; set; }

        [DisplayName("Tiene adecuación")]
        public bool tiene_adecuacion { get; set; }

        [DisplayName("Tiene discapacidad")]
        public bool tiene_discapacidad { get; set; }

        [DisplayName("Contacto de emergencia")]
        public string contactoEmergencia { get; set; }

        [DisplayName("Dirección")]
        public string direccion { get; set; }

        [DisplayName("¿Es estudiante de nuevo ingreso?")]
        public bool esNuevoIngreso { get; set; }
    }
}
