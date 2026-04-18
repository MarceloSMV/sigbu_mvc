using System.ComponentModel.DataAnnotations.Schema;

namespace sigbu_mvc.Models;

[Table("areas")]
public class Area
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public string responsable { get; set; } = null!;

    public string? ubicacion { get; set; }

    public string? descripcion { get; set; }

}