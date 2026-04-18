using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace sigbu_mvc.Controllers
{
    public class CerrarSesionController : Controller
    {
        // Acción única: Cerrar Sesión
        // GET: /CerrarSesion/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirigimos al LoginController para que muestre la pantalla de entrada
            return RedirectToAction("Login", "Login");
        }
    }
}