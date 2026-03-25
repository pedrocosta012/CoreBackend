using Microsoft.Extensions.Options;
using Resend;

namespace CoreBackend.Infrastructure.Email;

internal sealed class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ResendSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IResend resend,
        IOptions<ResendSettings> settings,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _settings = settings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken)
    {
        var apiToken = _configuration["RESEND:APITOKEN"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            _logger.LogWarning("RESEND__APITOKEN nao definido. Email de reset nao enviado para {Email}.", toEmail);
            return;
        }

        var message = new EmailMessage
        {
            From = _settings.FromEmail,
            Subject = "Recuperacao de senha",
            HtmlBody = $"<p>Use este link para redefinir sua senha:</p><p><a href=\"{resetLink}\">{resetLink}</a></p>"
        };
        message.To.Add(toEmail);

        try
        {
            await _resend.EmailSendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Falha ao enviar email de reset para {Email}.", toEmail);
        }
    }
}

