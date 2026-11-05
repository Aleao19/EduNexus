
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Prematriculas
{
    public class PrematriculaDto : IValidatableObject
    {
        public int id_prematricula { get; set; }

        // IDENTIFICACIÓN
        [Required(
            ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(
            50,
            ErrorMessage = "La identificación no puede superar los 50 caracteres.")]
        [DisplayName("Identificación")]
        public string identificacion { get; set; }

        // NOMBRE
        [Required(
            ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(
            100,
            ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [DisplayName("Nombre")]
        public string nombre { get; set; }

        // PRIMER APELLIDO
        [Required(
            ErrorMessage = "El primer apellido es obligatorio.")]
        [StringLength(
            100,
            ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
        [DisplayName("Primer apellido")]
        public string apellido1 { get; set; }

        // SEGUNDO APELLIDO
        [StringLength(
            100,
            ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
        [DisplayName("Segundo apellido")]
        public string apellido2 { get; set; }

        // GÉNERO
        [Required(
            ErrorMessage = "Seleccione el género del estudiante.")]
        [DisplayName("Género")]
        public string genero { get; set; }

        // GRADO SOLICITADO
        [Required(
            ErrorMessage = "Seleccione el grado solicitado.")]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Seleccione un grado válido.")]
        [DisplayName("Grado solicitado")]
        public int grado { get; set; }

        // TELÉFONO
        [StringLength(
            30,
            ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
        [RegularExpression(
            @"^[0-9]{8}$",
            ErrorMessage = "El teléfono debe contener exactamente 8 dígitos.")]
        [DisplayName("Teléfono")]
        public string telefono { get; set; }

        // FECHA DE NACIMIENTO
        [Required(
            ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayName("Fecha de nacimiento")]
        public DateTime? fecha_nacimiento { get; set; }

        // ADECUACIÓN EDUCATIVA
        [DisplayName("Tiene adecuación")]
        public bool tiene_adecuacion { get; set; }

        // DISCAPACIDAD
        [DisplayName("Tiene discapacidad")]
        public bool tiene_discapacidad { get; set; }

        // CONTACTO DE EMERGENCIA
        [StringLength(
            150,
            ErrorMessage = "El contacto de emergencia no puede superar los 150 caracteres.")]
        [DisplayName("Contacto de emergencia")]
        public string contactoEmergencia { get; set; }

        // DIRECCIÓN
        [StringLength(
            500,
            ErrorMessage = "La dirección no puede superar los 500 caracteres.")]
        [DisplayName("Dirección")]
        public string direccion { get; set; }

        // NUEVO INGRESO
        [DisplayName("¿Es estudiante de nuevo ingreso?")]
        public bool esNuevoIngreso { get; set; }

        // RMPR-08-002
        // ESTADO DE PREMATRÍCULA
        [DisplayName("Estado")]
        public string estado { get; set; }

        // FECHA DE PREMATRÍCULA
        [DisplayName("Fecha de prematrícula")]
        public DateTime fecha_prematricula { get; set; }

        // NOMBRE DEL GRADO
        [DisplayName("Grado")]
        public string nombre_grado { get; set; }

        // ==========================================
        // RMPR-08-007
        // RENOVACIÓN ANUAL DE MATRÍCULA
        // ==========================================

        // PERÍODO LECTIVO DE DESTINO
        // Ejemplo: renovación realizada en 2026
        // para el curso lectivo 2027.
        [DisplayName("Período lectivo")]
        public int? fk_id_calendario { get; set; }

        // NOMBRE DEL PERÍODO LECTIVO
        // Ejemplo: "Curso lectivo 2027".
        [DisplayName("Nombre del período lectivo")]
        public string nombre_calendario { get; set; }

        // ==========================================
        // RMPR-08-008
        // CUOTA VOLUNTARIA
        // ==========================================

        [DisplayName("Cuota voluntaria")]
        public decimal cuota_voluntaria { get; set; }

        // ==========================================
        // VALIDACIONES ADICIONALES
        // ==========================================

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // VALIDAR FECHA DE NACIMIENTO
            if (fecha_nacimiento.HasValue &&
                fecha_nacimiento.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "La fecha de nacimiento no puede ser futura.",
                    new[] { "fecha_nacimiento" }
                );
            }

            // VALIDAR IDENTIFICACIÓN
            if (identificacion != null &&
                string.IsNullOrWhiteSpace(identificacion))
            {
                yield return new ValidationResult(
                    "La identificación es obligatoria.",
                    new[] { "identificacion" }
                );
            }

            // VALIDAR NOMBRE
            if (nombre != null &&
                string.IsNullOrWhiteSpace(nombre))
            {
                yield return new ValidationResult(
                    "El nombre es obligatorio.",
                    new[] { "nombre" }
                );
            }

            // VALIDAR PRIMER APELLIDO
            if (apellido1 != null &&
                string.IsNullOrWhiteSpace(apellido1))
            {
                yield return new ValidationResult(
                    "El primer apellido es obligatorio.",
                    new[] { "apellido1" }
                );
            }

            // VALIDAR SEGUNDO APELLIDO
            if (apellido2 != null &&
                string.IsNullOrWhiteSpace(apellido2))
            {
                yield return new ValidationResult(
                    "El segundo apellido no puede contener únicamente espacios.",
                    new[] { "apellido2" }
                );
            }

            // VALIDAR GÉNERO
            if (!string.IsNullOrWhiteSpace(genero))
            {
                bool generoValido =
                    string.Equals(
                        genero.Trim(),
                        "Femenino",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        genero.Trim(),
                        "Masculino",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        genero.Trim(),
                        "Otro",
                        StringComparison.OrdinalIgnoreCase);

                if (!generoValido)
                {
                    yield return new ValidationResult(
                        "Seleccione un género válido.",
                        new[] { "genero" }
                    );
                }
            }

            // VALIDAR CONTACTO DE EMERGENCIA
            if (contactoEmergencia != null &&
                string.IsNullOrWhiteSpace(contactoEmergencia))
            {
                yield return new ValidationResult(
                    "El contacto de emergencia no puede contener únicamente espacios.",
                    new[] { "contactoEmergencia" }
                );
            }

            // VALIDAR DIRECCIÓN
            if (direccion != null &&
                string.IsNullOrWhiteSpace(direccion))
            {
                yield return new ValidationResult(
                    "La dirección no puede contener únicamente espacios.",
                    new[] { "direccion" }
                );
            }

            // VALIDAR CUOTA VOLUNTARIA
            if (cuota_voluntaria < 0)
            {
                yield return new ValidationResult(
                    "La cuota voluntaria no puede ser negativa.",
                    new[] { "cuota_voluntaria" }
                );
            }
        }
    }
}
