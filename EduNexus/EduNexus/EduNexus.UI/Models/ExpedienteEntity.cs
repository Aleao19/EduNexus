namespace EduNexus.UI.Models.Identity
{
    // Tabla: expediente_estudiantes
    // Los datos personales del estudiante (nombre, identificación, género, etc.)
    // se leen de la prematrícula ligada (fk_id_matricula); no se duplican aquí.
    public class ExpedienteEntity
    {
        public int id_expediente { get; set; }
        public int fk_id_matricula { get; set; }
        public string fk_id_usuario { get; set; }
        public string correo { get; set; }
        public string grupo_sanguineo { get; set; }
        public string alergias { get; set; }

        // Columna contacto_emergencia
        public string contactoEmergencia { get; set; }
    }
}
