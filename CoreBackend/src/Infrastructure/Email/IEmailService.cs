namespace CoreBackend.Infrastructure.Email;

internal interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken);
}

