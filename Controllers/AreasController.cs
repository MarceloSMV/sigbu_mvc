using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using sigbu_mvc.ViewModels;

namespace sigbu_mvc.Controllers
{
    [Authorize]
    public class AreasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AreasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. URL: /AdministrarAreas
        public IActionResult Index()
        {
            return View("AdministrarAreas");
        }

        // GET: /Areas/Autocompletar
        // GET: Areas/Autocompletar
        [HttpGet]
        public async Task<IActionResult> Autocompletar(string term, string campo)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<string>());

            term = term.ToLower();
            IQueryable<string> query = Enumerable.Empty<string>().AsQueryable();

            switch (campo)
            {
                case "nombre":
                    query = _context.Areas.Where(a => a.nombre.ToLower().Contains(term)).Select(a => a.nombre);
                    break;
                case "responsable":
                    query = _context.Areas.Where(a => a.responsable.ToLower().Contains(term)).Select(a => a.responsable);
                    break;
                case "ubicacion":
                    query = _context.Areas.Where(a => a.ubicacion != null && a.ubicacion.ToLower().Contains(term)).Select(a => a.ubicacion!);
                    break;
            }
            return Json(await query.Distinct().Take(10).ToListAsync());
        }

        // MÉTODO 2: Para Editar/Eliminar (Devuelve el OBJETO completo con ID)
        // ESTE ES EL QUE FALTABA
        [HttpGet]
        public async Task<IActionResult> BuscarAreas(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var resultados = await _context.Areas
                .Where(a => a.nombre.ToLower().Contains(term.ToLower()))
                .Take(10)
                .Select(a => new { a.id, a.nombre, a.responsable, a.ubicacion, a.descripcion })
                .ToListAsync();

            return Json(resultados);
        }

        // 2. URL: /AdministrarAreas/Buscar
        public async Task<IActionResult> Buscar(string nombre, string responsable, string ubicacion)
        {
            ViewData["Title"] = "Consultar Áreas";
            ViewData["Modo"] = "Lectura";

            var query = _context.Areas.AsQueryable();

            if (!string.IsNullOrEmpty(nombre)) query = query.Where(a => a.nombre.Contains(nombre));
            if (!string.IsNullOrEmpty(responsable)) query = query.Where(a => a.responsable.Contains(responsable));
            if (!string.IsNullOrEmpty(ubicacion)) query = query.Where(a => a.ubicacion.Contains(ubicacion));

            ViewData["filtroNombre"] = nombre;
            ViewData["filtroResponsable"] = responsable;
            ViewData["filtroUbicacion"] = ubicacion;

            var resultados = await query.ToListAsync();

            // Carga explicitamente Views/AdministrarAreas/BuscarAreas.cshtml
            return View("BuscarAreas", resultados);
        }

        // 3. URL: /AdministrarAreas/Editar
        public IActionResult Editar()
        {
            // Carga explicitamente Views/AdministrarAreas/EditarAreas.cshtml
            return View("EditarAreas", new List<AreaSolicitudViewModel>());
        }

        // 4. URL: /AdministrarAreas/Eliminar
        public IActionResult Eliminar()
        {
            // Carga explicitamente Views/AdministrarAreas/EliminarAreas.cshtml
            return View("EliminarAreas", new List<AreaSolicitudViewModel>());
        }

        // 5. URL: /AdministrarAreas/Agregar
        public IActionResult Agregar()
        {
            // Carga explicitamente Views/AdministrarAreas/AgregarAreas.cshtml
            return View("AgregarAreas", new List<AreaSolicitudViewModel> { new AreaSolicitudViewModel() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(List<AreaSolicitudViewModel> modelos, string sustento)
        {
            if (!ModelState.IsValid) return View("AgregarAreas", modelos);

            await CrearSolicitud(modelos, "Agregar", sustento);

            // Este ya estaba correcto, lo dejamos como referencia
            TempData["Mensaje"] = $"Solicitud generada para {modelos.Count} nueva(s) area(s).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(List<AreaSolicitudViewModel> modelos, string sustento)
        {
            if (!ModelState.IsValid || modelos.Count == 0)
            {
                if (modelos.Count == 0) ModelState.AddModelError("", "Debe agregar al menos un area.");
                return View("EditarAreas", modelos);
            }
            await CrearSolicitud(modelos, "Editar", sustento);

            // CAMBIO AQUÍ: Formato de mensaje estandarizado
            TempData["Mensaje"] = $"Solicitud generada para editar {modelos.Count} area(s).";

            // Esto redirige a Index(), que carga la vista "AdministrarAreas"
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(List<AreaSolicitudViewModel> modelos, string sustento)
        {
            if (modelos == null || modelos.Count == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar areas para dar de baja.");
                return View("EliminarAreas", modelos);
            }
            await CrearSolicitud(modelos, "Eliminar", sustento);

            // CAMBIO AQUÍ: Formato de mensaje estandarizado
            TempData["Mensaje"] = $"Solicitud generada para eliminar {modelos.Count} area(s).";

            // Esto redirige a Index(), que carga la vista "AdministrarAreas"
            return RedirectToAction(nameof(Index));
        }

        private async Task CrearSolicitud(List<AreaSolicitudViewModel> modelos, string tipo, string? sustento)
        {
            if (modelos == null || !modelos.Any()) return;

            var userId = int.Parse(User.FindFirst("UsuarioId")?.Value ?? "0");

            var solicitud = new Solicitud
            {
                usuario_id = userId,
                categoria = "areas",
                tipo = tipo, 
                estado = "pendiente",
                fecha_creacion = DateTime.UtcNow,
                sustento = sustento,
                area_detalles = new List<SolicitudAreaDetalle>()
            };

            foreach (var item in modelos)
            {
                int? safeAreaId = item.area_id;
                if (tipo == "Agregar" || item.area_id == 0) safeAreaId = null;

                solicitud.area_detalles.Add(new SolicitudAreaDetalle
                {
                    area_id = safeAreaId,
                    nombre = item.nombre,
                    responsable = item.responsable,
                    ubicacion = item.ubicacion,
                    descripcion = item.descripcion
                });
            }

            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync();
        }
    }
}