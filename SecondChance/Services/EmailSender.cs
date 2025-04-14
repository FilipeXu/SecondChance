using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SecondChance.Services
{
    /// <summary>
    /// Serviço responsável pelo envio de emails na plataforma.
    /// Implementa a interface IEmailSender para integração com o sistema de Identity.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmailAddress;
        private readonly string _fromEmailPassword;

        /// <summary>
        /// Construtor do EmailSender.
        /// Inicializa as configurações necessárias para o envio de emails.
        /// </summary>
        /// <param name="configuration">Configurações da aplicação</param>
        /// <exception cref="ArgumentNullException">Lançada quando faltam configurações obrigatórias</exception>
        public EmailSender(IConfiguration configuration)
        {
            _smtpServer = configuration["EmailSettings:SmtpServer"] 
                ?? throw new ArgumentNullException("SmtpServer configuration is missing");
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] 
                ?? throw new ArgumentNullException("SmtpPort configuration is missing"));
            _fromEmailAddress = configuration["EmailSettings:FromEmail"] 
                ?? throw new ArgumentNullException("FromEmail configuration is missing");
            _fromEmailPassword = configuration["EmailSettings:FromPassword"] 
                ?? throw new ArgumentNullException("FromPassword configuration is missing");

        }

        /// <summary>
        /// Envia um email para um destinatário específico.
        /// </summary>
        /// <param name="email">Endereço de email do destinatário</param>
        /// <param name="subject">Assunto do email</param>
        /// <param name="htmlMessage">Conteúdo do email em formato HTML</param>
        /// <exception cref="InvalidOperationException">Lançada quando ocorre um erro no envio do email</exception>
        /// <returns>Task representando a conclusão do envio do email</returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmailAddress),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };
                message.To.Add(new MailAddress(email));

                using var client = new SmtpClient()
                {
                    Host = _smtpServer,
                    Port = _smtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_fromEmailAddress, _fromEmailPassword),
                    Timeout = 30000
                };

                ServicePointManager.ServerCertificateValidationCallback = 
                    delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                    { return true; };

                await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {               
                if (ex.Message.Contains("Authentication Required") || ex.Message.Contains("5.7.0"))
                {
                    throw new InvalidOperationException(
                        "Falha na autenticação do email. Por favor, verifique se o email e a senha de app estão corretos.", ex);
                }
                
                throw new InvalidOperationException(
                    "Não foi possível enviar o email de confirmação. Erro SMTP: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Ocorreu um erro inesperado ao enviar o email de confirmação: " + ex.Message, ex);
            }
        }
    }
}