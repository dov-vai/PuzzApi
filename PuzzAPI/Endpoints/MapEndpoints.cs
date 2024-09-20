using System.Data;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;
using Microsoft.IdentityModel.Tokens;
using PuzzAPI.ConnectionHandler.RoomManager;
using PuzzAPI.Data.Models;
using PuzzAPI.Data.Services;
using PuzzAPI.Utils;
using Host = PuzzAPI.ConnectionHandler.Types.Host;

namespace PuzzAPI.Endpoints;

public static class MapEndpoints
{
    public static WebApplication MapWebSocketEndpoint(this WebApplication app)
    {
        app.Map("/ws", async (HttpContext context, JwtUtils jwt, IRoomManager manager) =>
        {
            Debug.WriteLine("Received web socket request");
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();

            var jwtToken = context.Request.Cookies["token"];
            var authorized = false;
            if (!string.IsNullOrEmpty(jwtToken))
                try
                {
                    authorized = jwt.ValidateToken(jwtToken);
                }
                catch (SecurityTokenExpiredException)
                {
                    authorized = false;
                }

            var handler = new ConnectionHandler.ConnectionHandler(ws, manager, authorized);

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
                var tokens = await auth.Login(user);
                HttpUtils.SetTokenCookies(response, tokens, !app.Environment.IsDevelopment());
                return Results.Json(new UserInfo { Username = user.Username });
            }
            catch (InvalidCredentialException)
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

        app.MapGet("/refresh-token", async (HttpContext context, AuthService auth) =>
        {
            var refreshToken = context.Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            try
            {
                var tokens = await auth.RefreshToken(refreshToken);
                if (tokens == null)
                    return Results.Unauthorized();
                HttpUtils.SetTokenCookies(context.Response, tokens, !app.Environment.IsDevelopment());
                return Results.Ok();
            }
            catch (SecurityTokenExpiredException)
            {
                return Results.Unauthorized();
            }
        });

        app.MapGet("/logout", async (HttpResponse response) =>
        {
            HttpUtils.DeleteTokenCookies(response);
            return Results.Ok();
        });

        app.MapGet("/logout-sessions", async (HttpContext context, AuthService auth) =>
        {
            var refreshToken = context.Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            try
            {
                await auth.InvalidateRefreshToken(refreshToken);
                HttpUtils.DeleteTokenCookies(context.Response);
                return Results.Ok();
            }
            catch (SecurityTokenExpiredException)
            {
                return Results.Unauthorized();
            }
        });

        return app;
    }

    public static WebApplication MapInfoEndpoints(this WebApplication app)
    {
        app.MapGet("/user-info", async (HttpContext context, AuthService auth) =>
        {
            var jwtToken = context.Request.Cookies["token"];

            if (string.IsNullOrEmpty(jwtToken))
                return Results.Unauthorized();

            try
            {
                var user = await auth.Authenticate(jwtToken);
                if (user == null)
                    return Results.Unauthorized();
                return Results.Json(new UserInfo { Username = user.Username });
            }
            catch (SecurityTokenExpiredException)
            {
                return Results.Unauthorized();
            }
        });

        return app;
    }

    public static WebApplication MapGameEndpoints(this WebApplication app)
    {
        app.MapGet("/public-rooms",
            async (HttpContext context, IRoomManager manager) => { return Results.Json(manager.GetPublicRooms()); });

        app.MapPost("/host-game", async (HttpContext context, IRoomManager manager, Host host) =>
        {
            // TODO: authorization / guest system
            manager.CreateRoom(host.Title, host.Pieces, host.Public, host.Guests, out var roomId);

            return Results.Json(new { RoomId = roomId });
        });

        return app;
    }
}