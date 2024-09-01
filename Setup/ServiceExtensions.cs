using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuzzAPI.ConnectionHandler.RoomManager;
using PuzzAPI.Data.Contexts;
using PuzzAPI.Data.Repositories;
using PuzzAPI.Data.Services;
using PuzzAPI.Utils;

namespace PuzzAPI.Setup;

public static class ServiceExtensions
{
    public static string AllowLocalHostCorsRule = "_allowLocalHost";

    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSingleton<IRoomManager, RoomManager>();
        services.AddDbContext<UserContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("UserContext")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<JwtUtils>();
        services.AddScoped<AuthService>();
        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(AllowLocalHostCorsRule, policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static IServiceCollection SetupJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var privateKey = File.ReadAllText(configuration["Jwt:PrivateKeyFile"]);
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey.ToCharArray());
        var rsaKey = new RsaSecurityKey(rsa);
        services.AddSingleton(new RsaKeyProvider(rsaKey));

        services.AddAuthentication(auth =>
        {
            auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(auth =>
        {
            auth.RequireHttpsMetadata = false;
            auth.SaveToken = true;
            auth.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = rsaKey,
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        return services;
    }
}