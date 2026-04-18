using System.ComponentModel.DataAnnotations;

namespace sigbu_mvc.ViewModels
{
    public class AreaSolicitudViewModel
    {
        public int? area_id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string nombre { get; set; } = null!;

        [Required(ErrorMessage = "El responsable es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string responsable { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")] 
        public string? ubicacion { get; set; }

        public string? descripcion { get; set; }

        // 
        //Control interno: agregar editar eliminar
        public string tipo_solicitud { get; set; } = "Agregar";
    }
}