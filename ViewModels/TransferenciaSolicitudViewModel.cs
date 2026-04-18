using sigbu_mvc.Models;

namespace sigbu_mvc.ViewModels
{
    public class TransferenciaSolicitudViewModel
    {
        public int AreaOrigenId { get; set; }
        public int AreaDestinoId { get; set; }
        public string Sustento { get; set; } = string.Empty;

        public IEnumerable<Area>? ListaAreas { get; set; }
        public List<int> BienesSeleccionados { get; set; } = new List<int>();
    }
}