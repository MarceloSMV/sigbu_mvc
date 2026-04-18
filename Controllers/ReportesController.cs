using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Data;
using sigbu_mvc.Models;
using sigbu_mvc.ViewModels;

namespace sigbu_mvc.Controllers
{
    // Permitimos acceso a todos los roles para que puedan ver los reportes
    [Authorize(Roles = "Jefe,Administrador,Trabajador")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método Index actualizado con el parámetro 'estado'
        public async Task<IActionResult> Index(string filtro = "Bienes", string estado = "Todo", string? codigo = null, string? serie = null, int? areaId = null)
        {
            var vm = new ReportesViewModel
            {
                TabActual = filtro,
                EstadoFiltro = estado, // Pasamos el estado seleccionado a la vista para mantener los botones activos
                FiltroCodigo = codigo,
                FiltroSerie = serie,
                FiltroAreaId = areaId
            };

            // LÓGICA 1: PESTAÑA INVENTARIO (Busca Bienes)
            if (filtro == "Inventario")
            {
                var query = _context.Bienes.Include(b => b.Area).AsQueryable();

                if (!string.IsNullOrEmpty(codigo)) query = query.Where(b => b.codigo != null && b.codigo.Contains(codigo));
                if (!string.IsNullOrEmpty(serie)) query = query.Where(b => b.serie != null && b.serie.Contains(serie));
                if (areaId.HasValue) query = query.Where(b => b.area_id == areaId.Value);

                vm.Inventario = await query.ToListAsync();
                vm.ListaAreas = new SelectList(_context.Areas, "id", "nombre", areaId);
            }
            // LÓGICA 2: PESTAÑAS DE HISTORIAL (Busca Solicitudes)
            else
            {
                var query = _context.Solicitudes
                    .Include(s => s.Usuario)
                    .Include(s => s.area_detalles)
                    .Include(s => s.bien_detalles)
                    .Include(s => s.transferencia_detalles).ThenInclude(d => d.AreaDestino)
                    .AsQueryable();

                // 1. Filtro por Categoría (Bienes, Areas, Transferencias)
                query = query.Where(s => s.categoria.ToLower() == filtro.ToLower());

                // 2. Filtro por Estado (NUEVO: Gestionado por los botones)
                if (estado != "Todo")
                {
                    query = query.Where(s => s.estado.ToLower() == estado.ToLower());
                }

                // NOTA: No aplicamos filtro de usuario, por lo que el Trabajador ve todo el historial.

                vm.Solicitudes = await query.OrderByDescending(s => s.fecha_creacion).ToListAsync();
            }

            return View("Reportes", vm);
        }

        // Acción para Exportar Excel usando los MISMOS filtros del Inventario
        public async Task<IActionResult> ExportarExcel(string? codigo, string? serie, int? areaId)
        {
            var query = _context.Bienes.Include(b => b.Area).AsQueryable();

            if (!string.IsNullOrEmpty(codigo)) query = query.Where(b => b.codigo.Contains(codigo));
            if (!string.IsNullOrEmpty(serie)) query = query.Where(b => b.serie.Contains(serie));
            if (areaId.HasValue) query = query.Where(b => b.area_id == areaId.Value);

            var bienes = await query.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Inventario Filtrado");

                // Estilos
                var headerStyle = worksheet.Range("A1:F1").Style;
                headerStyle.Font.Bold = true;
                headerStyle.Fill.BackgroundColor = XLColor.FromHtml("#e6fcf5"); // Mismo color verde suave del sistema

                worksheet.Cell(1, 1).Value = "Código";
                worksheet.Cell(1, 2).Value = "Serie";
                worksheet.Cell(1, 3).Value = "Descripción";
                worksheet.Cell(1, 4).Value = "Color";
                worksheet.Cell(1, 5).Value = "Estado";
                worksheet.Cell(1, 6).Value = "Área Actual";

                int row = 2;
                foreach (var item in bienes)
                {
                    worksheet.Cell(row, 1).Value = item.codigo;
                    worksheet.Cell(row, 2).Value = item.serie;
                    worksheet.Cell(row, 3).Value = item.descripcion;
                    worksheet.Cell(row, 4).Value = item.color;
                    worksheet.Cell(row, 5).Value = item.estado;
                    worksheet.Cell(row, 6).Value = item.Area?.nombre ?? "Sin asignar";
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Inventario_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }

        // Acción para previsualizar el PDF de una solicitud
        public async Task<IActionResult> PrevisualizarPdf(int id)
        {
            var solicitud = await _context.Solicitudes
               .Include(s => s.Usuario)
               .Include(s => s.area_detalles)
               .Include(s => s.bien_detalles).ThenInclude(d => d.area)
               .Include(s => s.transferencia_detalles).ThenInclude(d => d.Bien)
               .Include(s => s.transferencia_detalles).ThenInclude(d => d.AreaOrigen)
               .Include(s => s.transferencia_detalles).ThenInclude(d => d.AreaDestino)
               .FirstOrDefaultAsync(m => m.id == id);

            if (solicitud == null) return NotFound();
            return View("PlantillaReportePdf", solicitud);
        }
    }
}