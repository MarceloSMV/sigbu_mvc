using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sigbu_mvc.Models
{
    [Table("solicitudes")]
    public class Solicitud
    {
        [Key]
        public int id { get; set; }

        [Column("usuario_id")]
        public int usuario_id { get; set; }

        public string? sustento { get; set; }

        public string estado { get; set; } = null!;
        public DateTime fecha_creacion { get; set; }
        public string tipo { get; set; } = null!;
        public string categoria { get; set; } = null!;

        [ForeignKey("usuario_id")]
        public virtual Usuario Usuario { get; set; } = null!;

        public virtual ICollection<SolicitudBienDetalle> bien_detalles { get; set; } = new List<SolicitudBienDetalle>();
        public virtual ICollection<SolicitudAreaDetalle> area_detalles { get; set; } = new List<SolicitudAreaDetalle>();
        public virtual ICollection<SolicitudTransferenciaDetalle> transferencia_detalles { get; set; } = new List<SolicitudTransferenciaDetalle>();
    }
}