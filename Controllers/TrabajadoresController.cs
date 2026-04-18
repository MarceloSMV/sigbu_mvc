using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using System.Security.Cryptography;
using System.Text;

namespace sigbu_mvc.Controllers
{
    [Authorize(Roles = "Jefe")] 
    public class TrabajadoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrabajadoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> AdministrarTrabajadores()
        {
            var trabajadores = await _context.Usuarios
                .Where(u => u.rol == "Trabajador" || u.rol == "Jefe")
                .OrderBy(u => u.ap_paterno)
                .ToListAsync();

            return View(trabajadores);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPorDni(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var resultados = await _context.Usuarios
                .Where(u => (u.rol == "Trabajador" || u.rol == "Jefe") && u.dni.Contains(term))
                .OrderBy(u => u.ap_paterno)
                .Take(5)
                .Select(u => new
                {
                    label = $"[{u.dni}] [{u.nombres} {u.ap_paterno}] [{u.rol}]",
                    value = u.dni
                })
                .ToListAsync();

            return Json(resultados);
        }

        [HttpGet]
        public async Task<IActionResult> VerificarExistencia(string dni, string login, string correo, int? idExcluir)
        {
            var query = _context.Usuarios.AsQueryable();
            if (idExcluir.HasValue) query = query.Where(u => u.id != idExcluir.Value);

            bool dniExiste = await query.AnyAsync(u => u.dni == dni);
            bool loginExiste = await query.AnyAsync(u => u.usuario_login == login);
            bool correoExiste = await query.AnyAsync(u => u.correo == correo);

            return Json(new { dni = dniExiste, login = loginExiste, correo = correoExiste });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(Usuario usuario, string? passwordInput)
        {
            // 1. VALIDACIONES DE FORMATO
            // Si alguien burla el HTML 'maxlength', esto lo detiene.
            if (string.IsNullOrEmpty(usuario.dni) || usuario.dni.Length != 8)
            {
                return RedirectToAction(nameof(AdministrarTrabajadores));
            }

            // 2. VALIDACIÓN DE DUPLICADOS (Seguridad: DNI -> Login -> Correo)
            // Si el JS falla o es atacado, estas líneas protegen tu DB.

            if (await _context.Usuarios.AnyAsync(u => u.dni == usuario.dni))
            {
                return RedirectToAction(nameof(AdministrarTrabajadores));
            }

            if (await _context.Usuarios.AnyAsync(u => u.usuario_login == usuario.usuario_login))
            {
                return RedirectToAction(nameof(AdministrarTrabajadores));
            }

            if (await _context.Usuarios.AnyAsync(u => u.correo == usuario.correo))
            {
                return RedirectToAction(nameof(AdministrarTrabajadores));
            }

            // 3. CONFIGURACIÓN DEL USUARIO (Solo llega aquí si todo es válido)
            usuario.rol = "Trabajador";
            if (string.IsNullOrEmpty(usuario.estado)) usuario.estado = "Activo";

            string rawPassword = string.IsNullOrEmpty(passwordInput) ? usuario.dni : passwordInput;
            usuario.password_hash = ConvertToSha256(rawPassword);

            // Limpieza
            ModelState.Remove("rol");
            ModelState.Remove("estado");
            ModelState.Remove("password_hash");
            ModelState.Remove("usuario.rol");
            ModelState.Remove("usuario.estado");
            ModelState.Remove("usuario.password_hash");

            if (ModelState.IsValid)
            {
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Trabajador registrado correctamente."; // Este SÍ déjalo para confirmar el éxito
                return RedirectToAction(nameof(AdministrarTrabajadores));
            }

            // Error genérico de modelo inválido (sin detalles)
            return RedirectToAction(nameof(AdministrarTrabajadores));
        }

        [HttpPost]
        public async Task<IActionResult> Editar([FromBody] Usuario modelo)
        {
            // Limpiezas del ModelState (igual que antes)
            ModelState.Remove("password_hash");
            ModelState.Remove("rol");
            ModelState.Remove("usuario.password_hash");
            ModelState.Remove("usuario.rol");

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, mensaje = "Datos inválidos. Verifique campos vacíos." });
            }

            // 1. VALIDACIÓN DNI (Formato)
            if (modelo.dni.Length != 8)
            {
                return BadRequest(new { success = false, mensaje = "El DNI debe tener exactamente 8 caracteres." });
            }

            // 2. VALIDACIÓN DE DUPLICADOS (Secuencia Estricta: DNI -> Login -> Correo)
            // IMPORTANTE: Excluimos al propio usuario (u.id != modelo.id)

            // Prioridad 1: Verificar DNI
            if (await _context.Usuarios.AnyAsync(u => u.dni == modelo.dni && u.id != modelo.id))
            {
                return BadRequest(new { success = false, mensaje = "El DNI ya está registrado por otro trabajador." });
            }

            // Prioridad 2: Verificar Login
            if (await _context.Usuarios.AnyAsync(u => u.usuario_login == modelo.usuario_login && u.id != modelo.id))
            {
                return BadRequest(new { success = false, mensaje = "El Usuario Login ya existe." });
            }

            // Prioridad 3: Verificar Correo
            if (await _context.Usuarios.AnyAsync(u => u.correo == modelo.correo && u.id != modelo.id))
            {
                return BadRequest(new { success = false, mensaje = "El Correo ya está registrado." });
            }

            // 3. ACTUALIZACIÓN (Si pasó los filtros)
            var usuarioDb = await _context.Usuarios.FindAsync(modelo.id);
            if (usuarioDb == null) return NotFound(new { success = false, mensaje = "Usuario no encontrado." });

            usuarioDb.nombres = modelo.nombres;
            usuarioDb.ap_paterno = modelo.ap_paterno;
            usuarioDb.ap_materno = modelo.ap_materno;
            usuarioDb.dni = modelo.dni;
            usuarioDb.usuario_login = modelo.usuario_login;
            usuarioDb.correo = modelo.correo;
            
            if (usuarioDb.rol != "Jefe")
            {
                usuarioDb.estado = modelo.estado;
            } 


            try
            {
                _context.Update(usuarioDb);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, mensaje = "Datos actualizados correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = "Error al guardar: " + ex.Message });
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
    }
}