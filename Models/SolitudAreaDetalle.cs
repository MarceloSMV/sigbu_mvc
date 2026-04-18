using System.ComponentModel.DataAnnotations.Schema;

namespace sigbu_mvc.Models
{
    [Table("solicitud_area_detalles")]
    public class SolicitudAreaDetalle
    {
        public int id { get; set; }
        public int solicitud_id { get; set; }
        public string nombre { get; set; } = null!;
        public string responsable { get; set; } = null!;
        public string? ubicacion { get; set; }
        public string? descripcion { get; set; }
        public int? area_id { get; set; }
    }
}