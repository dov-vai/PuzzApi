using System.Data;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuzzAPI.ConnectionHandler;
using PuzzAPI.Data.Context;
using PuzzAPI.Data.Repository;
using PuzzAPI.Models;
using PuzzAPI.RoomManager;
using PuzzAPI.Services;
using PuzzAPI.Utils;

var builder = WebApplication.CreateBuilder(args);

var AllowLocalhost = "_allowLocalhost";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowLocalhost, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var privateKey = File.ReadAllText(builder.Configuration["Jwt:PrivateKeyFile"]);
using var rsa = RSA.Create();
rsa.ImportFromPem(privateKey.ToCharArray());
var rsaKey = new RsaSecurityKey(rsa);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRoomManager, RoomManager>();
builder.Services.AddSingleton(new RsaKeyProvider(rsaKey));
builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("UserContext")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<JwtUtils>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthentication(auth =>
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
builder.Services.AddAuthorization();

var app = builder.Build();

// create database if not already created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<UserContext>();
    context.Database.EnsureCreated();
}

app.UseWebSockets();
app.UseStaticFiles();
app.UseAuthentication();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors(AllowLocalhost);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.Map("/ws", async (HttpContext context, IRoomManager manager) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();

    var handler = new ConnectionHandler(ws, manager);

    await handler.Run();
});

app.MapPost("/login", async (HttpResponse response, AuthService auth, User user) =>
{
    try
    {
        var token = await auth.Authenticate(user);

        response.Cookies.Append("token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !app.Environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTime.UtcNow.AddMinutes(15)
        });

        return Results.Ok();
    }
    catch (InvalidCredentialException ex)
    {
        return Results.Unauthorized();
    }
});

app.MapPost("/register", async (AuthService auth, User user) =>
{
    try
    {
        await auth.Register(user);
        return Results.Ok();
    }
    catch (DuplicateNameException ex)
    {
        return Results.Conflict();
    }
});

app.MapFallbackToFile("index.html");

await app.RunAsync();