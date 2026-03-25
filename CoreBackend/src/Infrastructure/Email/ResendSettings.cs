namespace CoreBackend.Infrastructure.Email;

internal sealed class ResendSettings
{
    public string FromEmail { get; init; } = "noreply@valendo.dev";
    public string ResetPasswordBaseUrl { get; init; } = "http://localhost:3000/reset-password";
}

