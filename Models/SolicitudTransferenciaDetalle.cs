using System.ComponentModel.DataAnnotations.Schema;

namespace sigbu_mvc.Models
{
    [Table("solicitud_transferencia_detalles")]
    public class SolicitudTransferenciaDetalle
    {
        public int id { get; set; }

        public int solicitud_id { get; set; }
        public Solicitud? Solicitud { get; set; } // Propiedad de navegación

        public int bien_id { get; set; }
        public Bien? Bien { get; set; } // Propiedad de navegación

        public int area_origen_id { get; set; }
        public Area? AreaOrigen { get; set; } // Propiedad de navegación

        public int area_destino_id { get; set; }
        public Area? AreaDestino { get; set; } // Propiedad de navegación
    }
}