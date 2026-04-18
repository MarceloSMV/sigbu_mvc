using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sigbu_mvc.Data;
using System.Security.Cryptography;
using System.Text;

namespace sigbu_mvc.Controllers
{
    [Authorize] // Solo usuarios logueados
    public class CambiarContrasenaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CambiarContrasenaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /CambiarContrasena/CambiarContrasena
        [HttpGet]
        [Route("CambiarContrasena")]
        public IActionResult CambiarContrasena()
        {
            return View();
        }

        [HttpPost]
        [Route("CambiarContrasena")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(string password_actual, string nueva_password, string confirmar_password)
        {
            if (nueva_password != confirmar_password)
            {
                TempData["MensajeError"] = "La nueva contraseña y su confirmación no coinciden.";
                return View();
            }

            var usuarioIdClaim = User.FindFirst("UsuarioId");
            if (usuarioIdClaim == null) return RedirectToAction("Logout", "Login");

            int usuarioId = int.Parse(usuarioIdClaim.Value);
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return RedirectToAction("Logout", "Login");

            string hashActual = ConvertToSha256(password_actual);
            if (usuario.password_hash != hashActual)
            {
                TempData["MensajeError"] = "La contraseña actual es incorrecta.";
                return View();
            }

            usuario.password_hash = ConvertToSha256(nueva_password);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Mensaje"] = "Contraseña cambiada con éxito. Por favor ingrese con su nueva clave.";
            return RedirectToAction("Login", "Login");
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
    }
}