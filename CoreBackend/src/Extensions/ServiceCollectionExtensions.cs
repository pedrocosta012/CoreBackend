using System.Data;
using System.Reflection;
using CoreBackend.Auth;
using CoreBackend.Infrastructure.Database;
using CoreBackend.Infrastructure.Email;
using CoreBackend.Infrastructure.Events;
using CoreBackend.Infrastructure.Security;
using CoreBackend.Users;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;

namespace CoreBackend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IServiceCollection AddAuthorizationCoreBackend(this IServiceCollection services)
    {
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDbConnection>(_ =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection deve ser configurado.");
            }

            return new SqliteConnection(connectionString);
        });

        return services;
    }

    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<ResendSettings>(configuration.GetSection("Resend"));

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddScoped<AuthService>();

        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration["RESEND:APITOKEN"] ?? string.Empty;
        });
        services.AddTransient<IResend, ResendClient>();

        return services;
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IEventBus, EventBus>();

        var targetAssemblies = assemblies.Length > 0
            ? assemblies
            : [Assembly.GetExecutingAssembly()];

        var handlerInterfaceType = typeof(IEventHandler<>);

        foreach (var assembly in targetAssemblies)
        {
            var handlers = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .Select(i => new { ServiceType = i, ImplementationType = t }));

            foreach (var handler in handlers)
            {
                services.AddScoped(handler.ServiceType, handler.ImplementationType);
            }
        }

        return services;
    }
}
