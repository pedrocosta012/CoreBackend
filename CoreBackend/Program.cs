using System.Text;
using CoreBackend.Auth;
using CoreBackend.Categories;
using CoreBackend.Extensions;
using CoreBackend.Infrastructure.Database;
using CoreBackend.Users;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiDocumentation()
    .AddAuthorizationCoreBackend()
    .AddDatabaseConnection(builder.Configuration)
    .AddCoreInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT_SECRET deve ser definido via variavel de ambiente.");
}

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "CoreBackend";
        var audience = builder.Configuration["Jwt:Audience"] ?? "CoreBackend";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok()).ExcludeFromDescription();

app.MapCategoryEndpoints();
app.MapUserEndpoints();
app.MapAuthEndpoints();

app.Run();

public partial class Program;
