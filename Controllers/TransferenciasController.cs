using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using sigbu_mvc.ViewModels;
using System.Security.Claims;

namespace sigbu_mvc.Controllers
{
    [Authorize]
    public class TransferenciasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransferenciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> NuevaTransferencia()
        {
            var viewModel = new TransferenciaSolicitudViewModel
            {
                ListaAreas = await _context.Areas.OrderBy(a => a.nombre).ToListAsync()
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetBienesPorArea(int areaId)
        {
            var bienes = await _context.Bienes
                .Where(b => b.area_id == areaId)
                .Select(b => new {
                    b.id,
                    codigo = b.codigo ?? "-",
                    serie = b.serie ?? "-",
                    b.descripcion,
                    color = b.color ?? ""
                })
                .ToListAsync();

            return Json(bienes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaTransferencia(TransferenciaSolicitudViewModel model)
        {
            if (model.AreaOrigenId == model.AreaDestinoId)
            {
                ModelState.AddModelError("", "El área de origen y destino no pueden ser iguales.");
            }

            if (model.BienesSeleccionados == null || !model.BienesSeleccionados.Any())
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un bien para transferir.");
            }

            if (ModelState.IsValid)
            {
                var usuarioId = int.Parse(User.FindFirst("UsuarioId")?.Value ?? "0");

                var solicitud = new Solicitud
                {
                    usuario_id = usuarioId,
                    tipo = "Transferir",
                    categoria = "transferencias",  
                    fecha_creacion = DateTime.UtcNow,
                    estado = "pendiente",
                    sustento = model.Sustento
                };

                _context.Solicitudes.Add(solicitud);
                await _context.SaveChangesAsync();

                foreach (var bienId in model.BienesSeleccionados)
                {
                    var detalle = new SolicitudTransferenciaDetalle
                    {
                        solicitud_id = solicitud.id,
                        bien_id = bienId,
                        area_origen_id = model.AreaOrigenId,
                        area_destino_id = model.AreaDestinoId
                    };
                    _context.SolicitudTransferenciaDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud de transferencia enviada correctamente.";
                return RedirectToAction(nameof(NuevaTransferencia));
            }

            model.ListaAreas = await _context.Areas.OrderBy(a => a.nombre).ToListAsync();
            return View(model);
        }
    }
}