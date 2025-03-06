using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SecondChance.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmailAddress;
        private readonly string _fromEmailPassword;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _smtpServer = configuration["EmailSettings:SmtpServer"] 
                ?? throw new ArgumentNullException("SmtpServer configuration is missing");
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] 
                ?? throw new ArgumentNullException("SmtpPort configuration is missing"));
            _fromEmailAddress = configuration["EmailSettings:FromEmail"] 
                ?? throw new ArgumentNullException("FromEmail configuration is missing");
            _fromEmailPassword = configuration["EmailSettings:FromPassword"] 
                ?? throw new ArgumentNullException("FromPassword configuration is missing");
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {email}", email);
                _logger.LogDebug("SMTP Server: {server}, Port: {port}, From: {from}", 
                    _smtpServer, _smtpPort, _fromEmailAddress);

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
                    Timeout = 30000 // 30 seconds timeout
                };

                // Configurar validação de certificado SSL
                ServicePointManager.ServerCertificateValidationCallback = 
                    delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                    { return true; };

                _logger.LogInformation("Sending email...");
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {email}", email);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP Error sending email to {email}. Error: {message}", email, ex.Message);
                
                // Verificar se é erro de autenticação baseado na mensagem
                if (ex.Message.Contains("Authentication Required") || ex.Message.Contains("5.7.0"))
                {
                    _logger.LogError("Authentication failed. Please check email and password settings.");
                    throw new InvalidOperationException(
                        "Falha na autenticação do email. Por favor, verifique se o email e a senha de app estão corretos.", ex);
                }
                
                throw new InvalidOperationException(
                    "Não foi possível enviar o email de confirmação. Erro SMTP: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {email}. Error: {message}", email, ex.Message);
                throw new InvalidOperationException(
                    "Ocorreu um erro inesperado ao enviar o email de confirmação: " + ex.Message, ex);
            }
        }
    }
} 