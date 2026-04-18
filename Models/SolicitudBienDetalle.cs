using System.ComponentModel.DataAnnotations.Schema;

namespace sigbu_mvc.Models
{
    [Table("solicitud_bien_detalles")]
    public class SolicitudBienDetalle
    {
        public int id { get; set; }
        public int solicitud_id { get; set; }
        public int? bien_id { get; set; } 

        public string descripcion { get; set; } = null!;
        public string? codigo { get; set; }
        public string? serie { get; set; }
        public string? color { get; set; }
        public string? ubicacion { get; set; }
        public int area_id { get; set; }
        public Area area { get; set; } = null!;
        public string estado { get; set; } = "Activo"; 

    }
}
