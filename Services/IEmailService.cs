namespace sigbu_mvc.Services
{
    public interface IEmailService
    {
        Task EnviarCorreo(string destino, string asunto, string mensaje);
    }
}