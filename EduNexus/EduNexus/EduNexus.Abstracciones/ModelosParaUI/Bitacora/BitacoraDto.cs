using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduNexus.Abstracciones.ModelosParaUI.Bitacora
{
    public class BitacoraDto
    {
       
        public int id_evento { get; set; }
        [DisplayName("Tipo de evento")]
        public string tipoDeEvento { get; set; }
        [DisplayName("Descripción")]
        public string descripcionEvento { get; set; }
        [DisplayName("Fecha")]
        public DateTime fecha { get; set; }
        [DisplayName("Datos anteriores")]
        public string datosAnteriores { get; set; }
        [DisplayName("Datos posteriores")]
        public string datosPosteriores { get; set; }
        [DisplayName("Usuario")]
        public string Usuario { get; set; } //id
    }
}
