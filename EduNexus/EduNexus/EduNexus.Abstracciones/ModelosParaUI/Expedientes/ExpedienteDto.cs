
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Expedientes
{
    public class ExpedienteDto
    {
        // =========================================
        // REXP-03-001
        // DATOS GENERALES DEL EXPEDIENTE
        // =========================================

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

        [Required]
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

        // =========================================
        // REXP-03-004
        // CONTACTO DE EMERGENCIA
        // =========================================

        [Required(
            ErrorMessage = "Debe ingresar un contacto de emergencia."
        )]
        [DisplayName("Contacto de emergencia")]
        public string contactoEmergencia { get; set; }

        [DisplayName("Dirección")]
        public string direccion { get; set; }

        [Required]
        [EmailAddress]
        [DisplayName("Correo electrónico")]
        public string correo { get; set; }

        // =========================================
        // REXP-03-003
        // INFORMACIÓN MÉDICA DEL ESTUDIANTE
        // =========================================

        [Required(
            ErrorMessage = "Debe seleccionar el grupo sanguíneo."
        )]
        [DisplayName("Grupo sanguíneo")]
        public string grupo_sanguineo { get; set; }

        [Required(
            ErrorMessage = "Debe indicar las alergias o escribir Ninguna."
        )]
        [DisplayName("Alergias")]
        public string alergias { get; set; }
    }
}
