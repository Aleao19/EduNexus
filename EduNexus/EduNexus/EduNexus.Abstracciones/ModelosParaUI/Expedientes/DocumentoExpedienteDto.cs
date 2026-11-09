using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EduNexus.Abstracciones.ModelosParaUI.Expedientes
{
    public class DocumentoExpedienteDto
    {
        public int id_documento { get; set; }

        [Required(ErrorMessage = "Debe indicar el expediente.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un expediente válido.")]
        [DisplayName("Expediente")]
        public int id_expediente { get; set; }

        [Required(ErrorMessage = "Debe ingresar el nombre del documento.")]
        [StringLength(150, ErrorMessage = "El nombre del documento no puede superar los 150 caracteres.")]
        [DisplayName("Nombre del documento")]
        public string nombre_documento { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        [DisplayName("Descripción")]
        public string descripcion { get; set; }

        [DisplayName("Fecha de registro")]
        public DateTime fecha_registro { get; set; }

        [DisplayName("Nombre del archivo")]
        public string nombre_archivo { get; set; }

        [DisplayName("Tipo de archivo")]
        public string tipo_archivo { get; set; }

        [DisplayName("Tamaño")]
        public int tamano_bytes { get; set; }

        [DisplayName("Registrado por")]
        public string registrado_por { get; set; }

        // Nombre del estudiante dueño del expediente (solo para mostrar)
        public string estudiante { get; set; }
    }
}
