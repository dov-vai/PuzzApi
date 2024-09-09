using System.Data;
using System.Net;
using System.Security.Authentication;
using PuzzAPI.ConnectionHandler.RoomManager;
using PuzzAPI.Data.Models;
using PuzzAPI.Data.Services;

namespace PuzzAPI.Endpoints;

public static class MapEndpoints
{
    public static WebApplication MapWebSocketEndpoint(this WebApplication app)
    {
        app.Map("/ws", async (HttpContext context, IRoomManager manager) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();

            var handler = new ConnectionHandler.ConnectionHandler(ws, manager);

            await handler.Run();
        });

        return app;
    }

    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login", async (HttpResponse response, AuthService auth, User user) =>
        {
            try
            {
                var token = await auth.Login(user);

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

        return app;
    }

    public static WebApplication MapInfoEndpoints(this WebApplication app)
    {
        app.MapGet("/user-info", async (HttpContext context, AuthService auth) =>
        {
            var jwtToken = context.Request.Cookies["token"];
            var user = await auth.Authenticate(jwtToken);

            if (user == null)
                return Results.Unauthorized();

            return Results.Json(new {Username = user.Username});
        });

        return app;
    }
}