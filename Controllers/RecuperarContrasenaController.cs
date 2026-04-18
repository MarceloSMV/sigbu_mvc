using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Services; // Necesario para IEmailService
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Text.Json;

namespace sigbu_mvc.Controllers
{
    public class RecuperarContrasenaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; 

        public RecuperarContrasenaController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult RestablecerContrasena() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(string correo, [FromForm(Name = "g-recaptcha-response")] string recaptchaToken)
        {
            if (string.IsNullOrEmpty(recaptchaToken))
            {
                TempData["MensajeError"] = "Por favor, completa el Captcha.";
                return View();
            }

            bool captchaValido = await VerificarRecaptcha(recaptchaToken);
            if (!captchaValido)
            {
                TempData["MensajeError"] = "Error de validación del Captcha. Inténtalo de nuevo.";
                return View();
            }

            var usuario = await _context.Usuarios
                .Where(u => u.correo == correo)
                .OrderBy(u => u.id)
                .FirstOrDefaultAsync();

            if (usuario != null && (usuario.estado == "Activo"))
            {
                string codigo = GenerarCodigoVerificacion(6);

                usuario.codigoreset = codigo;
                usuario.expiracioncodigo = DateTime.UtcNow.AddMinutes(3);
                await _context.SaveChangesAsync();

                try
                {
                    string asunto = "Código de Recuperación - SIGBU";
                    string cuerpo = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <h2 style='color: #0056b3;'>Solicitud de Restablecimiento</h2>
                <p>Hola, <strong>{usuario.nombres}</strong>.</p>
                <p>Has solicitado restablecer tu contraseña. Tu código de seguridad es:</p>
                <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; border-radius: 4px;'>
                    {codigo}
                </div>
                <p style='color: #666; font-size: 12px; margin-top: 20px;'>Este código es válido por 3 minutos.</p>
            </div>";

                    await _emailService.EnviarCorreo(usuario.correo, asunto, cuerpo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando correo a {usuario.correo}: {ex.Message}");
                }
            }

            TempData["Mensaje"] = "Si su cuenta existe y está activa, hemos enviado un código de 6 dígitos a su correo electrónico.";

            TempData["UsuarioEmail"] = correo;

            return RedirectToAction(nameof(IngresarCodigo));
        }


        [HttpGet]
        public IActionResult IngresarCodigo()
        {
            if (TempData["UsuarioEmail"] == null) return RedirectToAction(nameof(RestablecerContrasena));
            TempData.Keep("UsuarioEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IngresarCodigo(string codigo1, string codigo2, string codigo3, string codigo4, string codigo5, string codigo6)
        {
            string codigoIngresado = $"{codigo1}{codigo2}{codigo3}{codigo4}{codigo5}{codigo6}";
            string? email = TempData["UsuarioEmail"]?.ToString();
            TempData.Keep("UsuarioEmail");

            if (string.IsNullOrEmpty(email))
            {
                TempData["MensajeError"] = "Sesión expirada. Intente de nuevo.";
                return RedirectToAction(nameof(RestablecerContrasena));
            }

            var usuario = await _context.Usuarios
                .Where(u => u.correo == email)
                .OrderBy(u => u.id)
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                TempData["MensajeError"] = "Código no válido o expirado.";
                return View();
            }

            bool codigoCorrecto = usuario.codigoreset == codigoIngresado;
            bool tiempoValido = usuario.expiracioncodigo.HasValue && usuario.expiracioncodigo.Value > DateTime.UtcNow;

            if (codigoCorrecto && tiempoValido)
            {
                string resetToken = Guid.NewGuid().ToString("N");
                usuario.codigoreset = resetToken;
                usuario.expiracioncodigo = DateTime.UtcNow.AddMinutes(5);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(NuevaContrasena), new { token = resetToken });
            }
            else
            {
                TempData["MensajeError"] = "Código no válido o expirado.";
                return View();
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarCodigoAjax()
        {
            string mensajeSeguro = "Si su cuenta existe, se le envio un codigo de verificacion.";

            if (TempData.Peek("UsuarioEmail") != null)
            {
                string email = TempData.Peek("UsuarioEmail").ToString();

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.correo == email && u.estado == "Activo");

                if (usuario != null)
                {
                    try
                    {
                        string codigo = GenerarCodigoVerificacion(6);
                        usuario.codigoreset = codigo;
                        usuario.expiracioncodigo = DateTime.UtcNow.AddMinutes(3);
                        await _context.SaveChangesAsync();

                        string asunto = "Código de Recuperación - SIGBU (Reenvío)";
                        string cuerpo = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                    <h2 style='color: #0056b3;'>Solicitud de Restablecimiento</h2>
                    <p>Hola, <strong>{usuario.nombres}</strong>.</p>
                    <p>Este es tu nuevo código de seguridad:</p>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; border-radius: 4px;'>
                        {codigo}
                    </div>
                    <p style='color: #666; font-size: 12px; margin-top: 20px;'>Este código es válido por 3 minutos.</p>
                </div>";

                        await _emailService.EnviarCorreo(usuario.correo, asunto, cuerpo);
                    }
                    catch
                    {
                    }
                }
            }

            return Ok(new { mensaje = mensajeSeguro });
        }

        [HttpGet]
        public async Task<IActionResult> NuevaContrasena(string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                TempData["ResetToken"] = token;
                return RedirectToAction(nameof(NuevaContrasena));
            }

            if (TempData["ResetToken"] == null)
            {
                TempData["MensajeError"] = "Token no proporcionado o sesión caducada.";
                return RedirectToAction(nameof(RestablecerContrasena));
            }

            string tokenGuardado = TempData["ResetToken"].ToString();
            TempData.Keep("ResetToken");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.codigoreset == tokenGuardado);

            if (usuario != null && usuario.expiracioncodigo.HasValue)
            {
                if (usuario.expiracioncodigo.Value > DateTime.UtcNow)
                {
                    return View();
                }
            }

            TempData["MensajeError"] = "El enlace ha expirado o no es válido.";
            return RedirectToAction(nameof(RestablecerContrasena));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaContrasena(string nueva_contrasena, string confirmar_contrasena)
        {
            string? token = TempData["ResetToken"]?.ToString();

            if (string.IsNullOrEmpty(token))
            {
                TempData["MensajeError"] = "La sesión ha expirado. Por favor, solicite el código nuevamente.";
                return RedirectToAction(nameof(RestablecerContrasena));
            }

            if (nueva_contrasena != confirmar_contrasena)
            {
                TempData["MensajeError"] = "Las contraseñas no coinciden.";
                TempData.Keep("ResetToken");
                return View();
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.codigoreset == token);

            if (usuario == null || !usuario.expiracioncodigo.HasValue || usuario.expiracioncodigo.Value <= DateTime.UtcNow)
            {
                TempData["MensajeError"] = "El tiempo para cambiar la contraseña ha expirado o el token es inválido.";
                return RedirectToAction(nameof(RestablecerContrasena));
            }

            usuario.password_hash = ConvertToSha256(nueva_contrasena);

            usuario.codigoreset = null;
            usuario.expiracioncodigo = null;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "¡Contraseña actualizada con éxito! Por favor inicie sesión.";
            return RedirectToAction("Login", "Login");
        }

        // --- MÉTODOS PRIVADOS ---

        private async Task<bool> VerificarRecaptcha(string token)
        {
            string secretKey = "clave"; //tu clave secreta
            if (string.IsNullOrEmpty(token)) return false;

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}", null);
                    response.EnsureSuccessStatusCode();
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return jsonString.Contains("\"success\": true");
                }
                catch { return false; }
            }
        }

        private string ConvertToSha256(string texto)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }

        private string GenerarCodigoVerificacion(int longitud)
        {
            const string chars = "0123456789";
            // Usamos RandomNumberGenerator para generar un código criptográficamente seguro
            return string.Create(longitud, chars, (span, charSet) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = charSet[RandomNumberGenerator.GetInt32(charSet.Length)];
                }
            });
        }
    }
}