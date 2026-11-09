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
        // Los datos personales provienen de la prematrícula
        // (tabla prematricula); los largos máximos siguen
        // las columnas de la base de datos.
        // =========================================

        public int id_expediente { get; set; }

        [DisplayName("Matrícula del estudiante")]
        public int id_matricula { get; set; }

        public string id_usuario { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(50, ErrorMessage = "La identificación no puede superar los 50 caracteres.")]
        [DisplayName("Identificación")]
        public string identificacion { get; set; }

        [Required(ErrorMessage = "Debe ingresar el nombre del estudiante.")]
        [StringLength(256, ErrorMessage = "El nombre no puede superar los 256 caracteres.")]
        [DisplayName("Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "Debe ingresar el primer apellido.")]
        [StringLength(256, ErrorMessage = "El primer apellido no puede superar los 256 caracteres.")]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }

        [StringLength(256, ErrorMessage = "El segundo apellido no puede superar los 256 caracteres.")]
        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el género.")]
        [StringLength(100, ErrorMessage = "El género no puede superar los 100 caracteres.")]
        [DisplayName("Género")]
        public string genero { get; set; }

        [DisplayName("Grado")]
        public int grado { get; set; }

        [DisplayName("Grado")]
        public string nombre_grado { get; set; }

        [DisplayName("Teléfono")]
        public string telefono { get; set; }

        [DisplayName("Fecha de nacimiento")]
        [DataType(DataType.Date)]
        public DateTime fecha_nacimiento { get; set; }

        [DisplayName("Tiene adecuación")]
        public bool tiene_adecuacion { get; set; }

        [DisplayName("Tiene discapacidad")]
        public bool tiene_discapacidad { get; set; }

        [DisplayName("Dirección")]
        public string direccion { get; set; }

        // =========================================
        // REXP-03-004
        // CONTACTO DE EMERGENCIA (expediente_estudiantes.contacto_emergencia)
        // =========================================

        [Required(ErrorMessage = "Debe ingresar un contacto de emergencia.")]
        [StringLength(256, ErrorMessage = "El contacto de emergencia no puede superar los 256 caracteres.")]
        [DisplayName("Contacto de emergencia")]
        public string contactoEmergencia { get; set; }

        // Si se deja vacío al crear, se usa el correo del usuario estudiante.
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
        [DisplayName("Correo electrónico")]
        public string correo { get; set; }

        // =========================================
        // REXP-03-003
        // INFORMACIÓN MÉDICA DEL ESTUDIANTE
        // =========================================

        [Required(ErrorMessage = "Debe seleccionar el grupo sanguíneo.")]
        [StringLength(10, ErrorMessage = "El grupo sanguíneo no puede superar los 10 caracteres.")]
        [DisplayName("Grupo sanguíneo")]
        public string grupo_sanguineo { get; set; }

        [Required(ErrorMessage = "Debe indicar las alergias o escribir Ninguna.")]
        [StringLength(512, ErrorMessage = "Las alergias no pueden superar los 512 caracteres.")]
        [DisplayName("Alergias")]
        public string alergias { get; set; }
    }
}
