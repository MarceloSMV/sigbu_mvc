using System.ComponentModel.DataAnnotations.Schema;

namespace sigbu_mvc.Models
{
    [Table("bienes")]
    public class Bien
    {
        public int id { get; set; }
        public string? codigo { get; set; }  // único si no es null
        public string? serie { get; set; }   // único si no es null
        public string descripcion { get; set; } = null!;
        public string? color { get; set; }
        public string? ubicacion { get; set; }
        public int area_id { get; set; }
        public Area Area { get; set; } = null!; // navegación
        public string estado { get; set; } = null!;
    }
}