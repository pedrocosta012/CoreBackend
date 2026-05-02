namespace CoreBackend.Infrastructure.Email;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken);
}

