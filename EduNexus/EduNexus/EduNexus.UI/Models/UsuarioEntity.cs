namespace EduNexus.UI.Models.Identity
{
    public class UsuarioEntity
    {
        public string id_usuario { get; set; }
        public string email { get; set; }
        public string nombre { get; set; }
        public string apellido1 { get; set; }
        public string apellido2 { get; set; }
        public string identificacion { get; set; }
        public bool estado { get; set; }
        public string telefono { get; set; }
        public string rol { get; set; }
        public string password_hash { get; set; }
        public string security_stamp { get; set; }
        public bool email_confirmado { get; set; }
    }
}