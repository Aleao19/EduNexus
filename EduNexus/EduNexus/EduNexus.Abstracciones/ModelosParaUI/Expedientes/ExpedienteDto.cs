using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Expedientes
{
    public class ExpedienteDto
    {
        public int id_expediente { get; set; }
        public int id_matricula { get; set; }
        [Required]
        [DisplayName("Identificación")]
        public string identificacion { get; set; }
        [Required]
        [DisplayName("Nombre")]
        public string nombre { get; set; }
        [Required]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }
        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }
        [DisplayName("Género")]
        public string genero { get; set; }
        [DisplayName("Grado")]
        public int grado { get; set; }
        [DisplayName("Teléfono")]
        public string telefono { get; set; }
        [DisplayName("Fecha de nacimiento")]
        [DataType(DataType.Date)]
        public DateTime fecha_nacimiento { get; set; }
        [DisplayName("Tiene adecuación")]
        public bool tiene_adecuacion { get; set; }
        [DisplayName("Tiene discapacidad")]
        public bool tiene_discapacidad { get; set; }
        [DisplayName("Contacto de emergencia")]
        public string contactoEmergencia { get; set; }
        [DisplayName("Dirección")]
        public string direccion { get; set; }
    }
}