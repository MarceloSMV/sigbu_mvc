using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace sigbu_mvc.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. REDIRECCIÓN DE LA RAÍZ
        // Captura "localhost" y lo manda a "localhost/login"
        [HttpGet]
        [Route("")]
        public IActionResult EntradaRaiz()
        {
            return RedirectToAction(nameof(Login));
        }

        // 2. ACCIÓN PRINCIPAL DEL LOGIN
        // Define que la URL sea explícitamente "/login"
        [HttpGet]
        [Route("login")]
        public IActionResult Login()
        {
            // Si el usuario ya tiene sesión, redirigir al Dashboard correspondiente
            if (User.Identity!.IsAuthenticated)
            {
                if (User.IsInRole("Jefe")) return RedirectToAction(nameof(DashboardJefe));
                if (User.IsInRole("Trabajador")) return RedirectToAction(nameof(DashboardTrabajador));
                return RedirectToAction("Logout", "CerrarSesion");
            }
            return View(); // Busca Views/Login/Login.cshtml
        }

        // 3. POST DEL LOGIN
        // Mantiene la misma URL "/login" al enviar el formulario
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(string usuario_login, string password)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.usuario_login == usuario_login);

            if (usuario == null || usuario.password_hash != ConvertToSha256(password))
            {
                TempData["Mensaje"] = "Usuario o contraseña incorrectos";
                return View();
            }

            if (usuario.estado != "Activo")
            {
                TempData["Mensaje"] = "Su cuenta ha sido denegada. Contacte a un administrador.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.nombres),
                new Claim(ClaimTypes.Surname, usuario.ap_paterno),
                new Claim("UsuarioId", usuario.id.ToString()),
                new Claim(ClaimTypes.Role, usuario.rol),
                new Claim(ClaimTypes.Email, usuario.correo)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            if (usuario.rol == "Jefe") return RedirectToAction(nameof(DashboardJefe));
            if (usuario.rol == "Trabajador") return RedirectToAction(nameof(DashboardTrabajador));

            // Fallback
            return RedirectToAction(nameof(Login));
        }

        // ============================================================
        //  DASHBOARDS Y LOGOUT
        // ============================================================

        [Authorize(Roles = "Jefe")]
        [Route("DashboardJefe")]
        public IActionResult DashboardJefe()
        {
            return View();
        }

        [Authorize(Roles = "Trabajador")]
        [Route("DashboardTrabajador")]
        public IActionResult DashboardTrabajador()
        {
            return View();
        }


        // ============================================================
        //  UTILIDADES
        // ============================================================

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