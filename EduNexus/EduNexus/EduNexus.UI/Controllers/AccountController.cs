
using System;
using EduNexus.Abstracciones.ModelosParaUI.Usuarios;
using EduNexus.UI.Models;
using EduNexus.UI.Models.Identity;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;

namespace EduNexus.UI.Controllers
{
    public class AccountController : Controller
    {
        // Acción para mostrar el formulario de "Olvidé mi contraseña"
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // Acción POST para enviar el correo de recuperación
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = new EduNexusDbContext())
            {
                string email = model.Email.Trim();

                UsuarioEntity usuario = db.Usuarios.FirstOrDefault(
                    u => u.email == email);

                // Verificar si el correo está registrado
                if (usuario == null)
                {
                    ModelState.AddModelError(
                        "",
                        "El correo electrónico no está registrado.");

                    return View(model);
                }

                // Generar un token temporal
                string token = Guid.NewGuid().ToString("N");

                usuario.token_recuperacion = token;
                usuario.expira_token_recuperacion =
                    DateTime.Now.AddMinutes(30);

                db.SaveChanges();

                try
                {
                    // Enviar correo de recuperación
                    var fromAddress =
                        new MailAddress("noreply@edunexus.com");

                    var toAddress = new MailAddress(email);

                    string subject = "Recuperación de Contraseña";

                    string body =
                        $"Usa el siguiente token para cambiar tu contraseña: {token}";

                    using (var smtpClient = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,

                        // Tiempo máximo de espera: 15 segundos
                        Timeout = 15000,

                        Credentials = new System.Net.NetworkCredential(
                            "your-email@gmail.com",
                            "your-password")
                    })
                    {
                        using (var mailMessage =
                            new MailMessage(fromAddress, toAddress)
                            {
                                Subject = subject,
                                Body = body
                            })
                        {
                            smtpClient.Send(mailMessage);
                        }
                    }

                    ViewBag.Message =
                        "Un correo de recuperación ha sido enviado a tu dirección.";
                }
                catch (Exception)
                {
                    // Mostrar un mensaje si ocurre un error al enviar el correo
                    ModelState.AddModelError(
                        "",
                        "No se pudo enviar el correo. Por favor, intente nuevamente.");

                    return View(model);
                }
            }

            return View("ForgotPasswordSuccess");
        }

        // Acción para mostrar el formulario de cambio de contraseña
        [AllowAnonymous]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "El token es inválido.");
                return RedirectToAction("Login");
            }

            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity usuario = db.Usuarios.FirstOrDefault(
                    u => u.token_recuperacion == token &&
                         u.expira_token_recuperacion > DateTime.Now);

                if (usuario == null)
                {
                    ModelState.AddModelError(
                        "",
                        "El token es inválido o ha expirado.");

                    return RedirectToAction("Login");
                }

                ResetPasswordModel model =
                    new ResetPasswordModel { Token = token };

                ViewBag.Token = token;

                return View(model);
            }
        }

        // Acción POST para cambiar la contraseña
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = new EduNexusDbContext())
            {
                UsuarioEntity usuario = db.Usuarios.FirstOrDefault(
                    u => u.token_recuperacion == model.Token &&
                         u.expira_token_recuperacion > DateTime.Now);

                if (usuario == null)
                {
                    ModelState.AddModelError(
                        "",
                        "El token es inválido o ha expirado.");

                    return RedirectToAction("Login");
                }

                string hashedPassword =
                    ContrasennaHasher.Hash(model.NuevaContrasenna);

                usuario.password_hash = hashedPassword;
                usuario.token_recuperacion = null;
                usuario.expira_token_recuperacion = null;

                db.SaveChanges();

                ViewBag.Message =
                    "Tu contraseña ha sido cambiada con éxito.";
            }

            return View("ResetPasswordSuccess");
        }
    }
}
