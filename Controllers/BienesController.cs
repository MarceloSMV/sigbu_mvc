using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using sigbu_mvc.ViewModels;
using System.Security.Claims;

namespace sigbu_mvc.Controllers
{
    [Authorize]
    public class BienesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BienesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("AdministrarBienes");
        }

        // GET: Autocompletar (NUEVO: Para los inputs de BuscarBienes)
        [HttpGet]
        public async Task<IActionResult> Autocompletar(string term, string campo)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<string>());
            term = term.ToLower();

            IQueryable<string> query = Enumerable.Empty<string>().AsQueryable();

            if (campo == "codigo")
            {
                query = _context.Bienes
                    .Where(b => b.codigo != null && b.codigo.ToLower().Contains(term))
                    .Select(b => b.codigo!);
            }
            else if (campo == "serie")
            {
                query = _context.Bienes
                    .Where(b => b.serie != null && b.serie.ToLower().Contains(term))
                    .Select(b => b.serie!);
            }

            var resultados = await query.Distinct().Take(10).ToListAsync();
            return Json(resultados);
        }

        // GET: BuscarBienes (Existente: Para el buscador de Editar/Eliminar)
        [HttpGet]
        public async Task<IActionResult> BuscarBienes(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 3) return Json(new List<object>());

            var resultados = await _context.Bienes
                .Include(b => b.Area)
                .Where(b => b.descripcion.ToLower().Contains(term.ToLower()) ||
                            b.codigo.ToLower().Contains(term.ToLower()) ||
                            b.serie.ToLower().Contains(term.ToLower()))
                .Take(10)
                .Select(b => new
                {
                    b.id,
                    codigo = b.codigo ?? "",
                    serie = b.serie ?? "",
                    b.descripcion,
                    b.color,
                    b.ubicacion,
                    b.area_id,
                    area_nombre = b.Area.nombre,
                    b.estado
                })
                .ToListAsync();

            return Json(resultados);
        }

        // GET: Buscar (CORREGIDO: Mantiene filtros en pantalla)
        public async Task<IActionResult> Buscar(string codigo, string serie, int? area_id, string estado)
        {
            ViewData["Title"] = "Buscar Bienes";
            ViewData["Modo"] = "Lectura";

            var query = _context.Bienes.Include(b => b.Area).AsQueryable();

            if (!string.IsNullOrEmpty(codigo)) query = query.Where(b => b.codigo.Contains(codigo));
            if (!string.IsNullOrEmpty(serie)) query = query.Where(b => b.serie.Contains(serie));
            if (area_id.HasValue) query = query.Where(b => b.area_id == area_id.Value);
            if (!string.IsNullOrEmpty(estado)) query = query.Where(b => b.estado == estado);

            // CORRECCIÓN: Pasar los valores actuales a la vista
            ViewData["filtroCodigo"] = codigo;
            ViewData["filtroSerie"] = serie;

            // CORRECCIÓN: Los nombres de ViewBag deben coincidir con los de la vista
            ViewBag.Areas = new SelectList(_context.Areas, "id", "nombre", area_id);
            ViewBag.Estados = new SelectList(new List<string> { "Bueno", "Regular", "Malo" }, estado);

            return View("BuscarBienes", await query.ToListAsync());
        }

        // ... (Resto de métodos Agregar, Editar, Eliminar, VerificarExistencia, CrearSolicitud se mantienen igual) ...

        public IActionResult Agregar() { CargarListas(); return View("AgregarBienes", new List<BienSolicitudViewModel> { new BienSolicitudViewModel() }); }
        public IActionResult Editar() { CargarListas(); return View("EditarBienes", new List<BienSolicitudViewModel>()); }
        public IActionResult Eliminar() { return View("EliminarBienes", new List<BienSolicitudViewModel>()); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(List<BienSolicitudViewModel> modelos, string sustento)
        {
            foreach (var m in modelos)
            {
                if (string.IsNullOrEmpty(m.codigo) && string.IsNullOrEmpty(m.serie))
                    ModelState.AddModelError("", $"El bien '{m.descripcion}' debe tener Código o Serie.");
            }
            if (!ModelState.IsValid) { CargarListas(); return View("AgregarBienes", modelos); }
            await CrearSolicitud(modelos, "Agregar", sustento);
            TempData["Mensaje"] = $"Solicitud generada para agregar {modelos.Count} nuevo(s) bien(es).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(List<BienSolicitudViewModel> modelos, string sustento)
        {
            if (!ModelState.IsValid || modelos.Count == 0)
            {
                CargarListas();
                if (modelos.Count == 0) ModelState.AddModelError("", "Debe agregar al menos un bien.");
                return View("EditarBienes", modelos);
            }
            await CrearSolicitud(modelos, "Editar", sustento);
            TempData["Mensaje"] = $"Solicitud generada para editar {modelos.Count} bien(es).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(List<BienSolicitudViewModel> modelos, string sustento)
        {
            if (modelos == null || modelos.Count == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar bienes para dar de baja.");
                return View("EliminarBienes", modelos);
            }
            await CrearSolicitud(modelos, "Eliminar", sustento);
            TempData["Mensaje"] = $"Solicitud generada para eliminar {modelos.Count} bien(es).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> VerificarExistencia(string? codigo, string? serie, int? idExcluir)
        {
            bool codigoExiste = false;
            bool serieExiste = false;
            if (!string.IsNullOrEmpty(codigo))
            {
                var query = _context.Bienes.AsQueryable();
                if (idExcluir.HasValue) query = query.Where(b => b.id != idExcluir.Value);
                codigoExiste = await query.AnyAsync(b => b.codigo == codigo);
            }
            if (!string.IsNullOrEmpty(serie))
            {
                var query = _context.Bienes.AsQueryable();
                if (idExcluir.HasValue) query = query.Where(b => b.id != idExcluir.Value);
                serieExiste = await query.AnyAsync(b => b.serie == serie);
            }
            return Json(new { codigo = codigoExiste, serie = serieExiste });
        }

        private async Task CrearSolicitud(List<BienSolicitudViewModel> modelos, string tipo, string? sustento)
        {
            if (modelos == null || !modelos.Any()) return;
            var userId = int.Parse(User.FindFirst("UsuarioId")?.Value ?? "0");
            var solicitud = new Solicitud
            {
                usuario_id = userId,
                categoria = "bienes",
                tipo = tipo,
                estado = "pendiente",
                fecha_creacion = DateTime.UtcNow,
                sustento = sustento,
                bien_detalles = new List<SolicitudBienDetalle>()
            };
            foreach (var item in modelos)
            {
                int bienIdReal = (tipo == "Agregar") ? 0 : (item.bien_id ?? 0);
                solicitud.bien_detalles.Add(new SolicitudBienDetalle
                {
                    bien_id = bienIdReal == 0 ? null : bienIdReal,
                    codigo = item.codigo,
                    serie = item.serie,
                    descripcion = item.descripcion,
                    color = item.color,
                    ubicacion = item.ubicacion,
                    area_id = item.area_id,
                    estado = item.estado
                });
            }
            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync();
        }

        private void CargarListas()
        {
            ViewBag.Areas = new SelectList(_context.Areas, "id", "nombre");
            ViewBag.Estados = new SelectList(new List<string> { "Bueno", "Regular", "Malo" });
        }
    }
}