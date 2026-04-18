namespace sigbu_mvc.Models;

public class Usuario
{
    public int id { get; set; }
    public string nombres { get; set; } = null!;
    public string ap_paterno { get; set; } = null!;
    public string ap_materno { get; set; } = null!;
    public string dni { get; set; } = null!;
    public string usuario_login { get; set; } = null!;
    public string password_hash { get; set; } = null!;
    public string rol { get; set; } = null!;
    public string correo { get; set; } = null!;
    public string estado { get; set; } = null!;

    // parte de seguridad
    public string? codigoreset { get; set; }
    public DateTime? expiracioncodigo { get; set; }
}