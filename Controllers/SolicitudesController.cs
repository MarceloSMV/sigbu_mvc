using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using System.Security.Claims;

namespace sigbu_mvc.Controllers
{
    [Authorize(Roles = "Jefe")]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SolicitudesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filtro = "Todos")
        {
            var solicitudesQuery = _context.Solicitudes
                .Where(s => s.estado.ToLower() == "pendiente")
                .Include(s => s.Usuario)
                .Include(s => s.area_detalles)
                .Include(s => s.bien_detalles)
                .Include(s => s.transferencia_detalles).ThenInclude(d => d.Bien)
                .Include(s => s.transferencia_detalles).ThenInclude(d => d.AreaOrigen)
                .Include(s => s.transferencia_detalles).ThenInclude(d => d.AreaDestino)
                .OrderByDescending(s => s.fecha_creacion)
                .AsQueryable();

            // FILTROS CORREGIDOS (Capitalizados para mantener correlacion)
            if (filtro == "Area")
            {
                solicitudesQuery = solicitudesQuery.Where(s => s.categoria.ToLower() == "areas");
            }
            else if (filtro == "Bienes")
            {
                solicitudesQuery = solicitudesQuery.Where(s => s.categoria.ToLower() == "bienes");
            }
            else if (filtro == "Transferencias") 
            {
                solicitudesQuery = solicitudesQuery.Where(s => s.categoria.ToLower() == "transferencias");
            }

            var solicitudes = await solicitudesQuery.ToListAsync();
            ViewBag.FiltroActual = filtro;

            return View("AdministrarSolicitudes", solicitudes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.area_detalles)
                .Include(s => s.bien_detalles)
                .Include(s => s.transferencia_detalles)
                .FirstOrDefaultAsync(s => s.id == id);

            if (solicitud == null) return NotFound();

            string cat = solicitud.categoria.ToLower();
            string tipo = solicitud.tipo;

            if (cat == "areas")
            {
                foreach (var detalle in solicitud.area_detalles!)
                {
                    if (tipo == "Agregar")
                    {
                        _context.Areas.Add(new Area
                        {
                            nombre = detalle.nombre,
                            responsable = detalle.responsable,
                            ubicacion = detalle.ubicacion,
                            descripcion = detalle.descripcion
                        });
                    }
                    else if (tipo == "Editar")
                    {
                        var area = await _context.Areas.FindAsync(detalle.area_id);
                        if (area != null)
                        {
                            area.nombre = detalle.nombre; area.responsable = detalle.responsable;
                            area.ubicacion = detalle.ubicacion; area.descripcion = detalle.descripcion;
                            _context.Areas.Update(area);
                        }
                    }
                    else if (tipo == "Eliminar")
                    {
                        var area = await _context.Areas.FindAsync(detalle.area_id);
                        if (area != null) _context.Areas.Remove(area);
                    }
                }
            }
            else if (cat == "bienes")
            {
                foreach (var detalle in solicitud.bien_detalles!)
                {
                    if (tipo == "Agregar")
                    {
                        _context.Bienes.Add(new Bien
                        {
                            codigo = detalle.codigo,
                            serie = detalle.serie,
                            descripcion = detalle.descripcion,
                            color = detalle.color,
                            ubicacion = detalle.ubicacion,
                            area_id = detalle.area_id,
                            estado = detalle.estado
                        });
                    }
                    else if (tipo == "Editar")
                    {
                        var bien = await _context.Bienes.FindAsync(detalle.bien_id);
                        if (bien != null)
                        {
                            bien.codigo = detalle.codigo; bien.serie = detalle.serie;
                            bien.descripcion = detalle.descripcion; bien.color = detalle.color;
                            bien.ubicacion = detalle.ubicacion; bien.area_id = detalle.area_id;
                            bien.estado = detalle.estado; _context.Bienes.Update(bien);
                        }
                    }
                    else if (tipo == "Eliminar")
                    {
                        var bien = await _context.Bienes.FindAsync(detalle.bien_id);
                        if (bien != null) _context.Bienes.Remove(bien);
                    }
                }
            }
            else if (cat == "transferencias")
            {
                foreach (var detalle in solicitud.transferencia_detalles!)
                {
                    var bien = await _context.Bienes.FindAsync(detalle.bien_id);
                    if (bien != null)
                    {
                        bien.area_id = detalle.area_destino_id;
                        _context.Bienes.Update(bien);
                    }
                }
            }

            solicitud.estado = "aprobado";

            if (solicitud.fecha_creacion.Kind == DateTimeKind.Unspecified)
                solicitud.fecha_creacion = DateTime.SpecifyKind(solicitud.fecha_creacion, DateTimeKind.Utc);

            _context.Solicitudes.Update(solicitud);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Solicitud aprobada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await _context.Solicitudes.FindAsync(id);
            if (solicitud != null)
            {
                solicitud.estado = "rechazado";

                if (solicitud.fecha_creacion.Kind == DateTimeKind.Unspecified)
                    solicitud.fecha_creacion = DateTime.SpecifyKind(solicitud.fecha_creacion, DateTimeKind.Utc);

                _context.Solicitudes.Update(solicitud);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Solicitud rechazada.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}