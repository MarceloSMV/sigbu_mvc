using Microsoft.AspNetCore.Mvc.Rendering;
using sigbu_mvc.Models;

namespace sigbu_mvc.ViewModels
{
    public class ReportesViewModel
    {
        public string TabActual { get; set; } = "Bienes";

        public string EstadoFiltro { get; set; } = "Todo";

        public List<Solicitud> Solicitudes { get; set; } = new List<Solicitud>();
        public List<Bien> Inventario { get; set; } = new List<Bien>();

        public string? FiltroCodigo { get; set; }
        public string? FiltroSerie { get; set; }
        public int? FiltroAreaId { get; set; }
        public SelectList? ListaAreas { get; set; }
    }
}