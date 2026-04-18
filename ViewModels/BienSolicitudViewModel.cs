using System.ComponentModel.DataAnnotations;

namespace sigbu_mvc.ViewModels
{
    public class BienSolicitudViewModel
    {
        public int? bien_id { get; set; }

        [StringLength(40, ErrorMessage = "Máximo 40 caracteres")]
        public string? codigo { get; set; }

        [StringLength(40, ErrorMessage = "Máximo 40 caracteres")]
        public string? serie { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string descripcion { get; set; } = null!;

        [StringLength(40, ErrorMessage = "Máximo 40 caracteres")]
        public string? color { get; set; }

        [StringLength(255, ErrorMessage = "Máximo 255 caracteres")]
        public string? ubicacion { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un área")]
        public int area_id { get; set; }

        [Required(ErrorMessage = "Seleccione un estado")]
        public string estado { get; set; } = "Bueno";

        public string tipo_solicitud { get; set; } = "Agregar";
    }
}