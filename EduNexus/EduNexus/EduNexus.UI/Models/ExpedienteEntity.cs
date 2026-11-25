
namespace EduNexus.UI.Models.Identity
{
    // REXP-03-001
    // Tabla de MySQL: expediente_estudiantes

    public class ExpedienteEntity
    {
        // Identificador principal del expediente
        public int id_expediente { get; set; }

        // Relacion con la tabla prematricula
        public int fk_id_matricula { get; set; }

        // Relacion con la tabla usuarios
        public string fk_id_usuario { get; set; }

        // Nuevos campos del expediente
        public string correo { get; set; }

        public string grupo_sanguineo { get; set; }

        public string alergias { get; set; }

        public string contactoEmergencia { get; set; }
    }
}
