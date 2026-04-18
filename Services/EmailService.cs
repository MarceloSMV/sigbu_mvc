using System.Net;
using System.Net.Mail;

namespace sigbu_mvc.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreo(string destino, string asunto, string cuerpo)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            string host = emailSettings["Host"];
            int port = int.Parse(emailSettings["Port"]);
            string emailFrom = emailSettings["Email"];
            string password = emailSettings["Password"];

            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(emailFrom, password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailFrom),
                    Subject = asunto,
                    Body = cuerpo,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(destino);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}