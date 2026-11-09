using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models.Identity;
using Newtonsoft.Json;
using System;
using System.Web;

namespace EduNexus.UI.Models
{
    // Trazabilidad: registra en la tabla bitacora quién hizo qué y cuándo,
    // con los datos anteriores y posteriores en JSON.
    // Se llama ANTES de db.SaveChanges() para que el cambio y su registro
    // se guarden juntos (si uno falla, no se guarda ninguno).
    public static class Auditoria
    {
        public static void Registrar(EduNexusDbContext db, string tipo, string descripcion,
            object antes = null, object despues = null, string idUsuario = null)
        {
            if (idUsuario == null)
            {
                var actual = HttpContext.Current != null && HttpContext.Current.Session != null
                    ? HttpContext.Current.Session[AutorizarAdministradorAttribute.ClaveSesion] as UsuarioDto
                    : null;
                idUsuario = actual != null ? actual.id_usuario : null;
            }

            // bitacora.fk_id_usuario es obligatorio: sin usuario identificado no se registra.
            if (string.IsNullOrEmpty(idUsuario))
            {
                return;
            }

            db.Bitacora.Add(new BitacoraEntity
            {
                tipo_de_evento = Recortar(tipo, 100),
                descripcion_evento = Recortar(descripcion, 512),
                fecha = DateTime.Now,
                datos_anteriores = antes == null ? null : JsonConvert.SerializeObject(antes),
                datos_posteriores = despues == null ? null : JsonConvert.SerializeObject(despues),
                fk_id_usuario = idUsuario
            });
        }

        private static string Recortar(string texto, int maximo)
        {
            texto = texto ?? "";
            return texto.Length <= maximo ? texto : texto.Substring(0, maximo);
        }
    }
}
