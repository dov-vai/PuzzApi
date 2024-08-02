using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PuzzAPI.RoomManager;
using PuzzAPI.Types;
using Host = PuzzAPI.Types.Host;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRoomManager, RoomManager>();

var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Map("/ws", async (HttpContext context, IRoomManager manager) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();

    string? roomId = null;
    string? peerId = null;

    if (context.Request.Query.ContainsKey("join"))
    {
        roomId = context.Request.Query["join"][0];

        if (roomId != null)
        {
            peerId = manager.AddPeer(roomId, ws);
            await manager.SendMessageAsync(
                roomId,
                peerId,
                JsonSerializer.Serialize(new Connected { Type = "connected", SocketId = peerId })
            );

            await manager.BroadcastAsync(
                roomId,
                JsonSerializer.Serialize(new ReceiveInit { Type = "receiveInit", SocketId = peerId }),
                ws
            );
        }
    }

    await manager.ReceiveMessageAsync(ws, async (result, buf) =>
    {
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var msg = Encoding.UTF8.GetString(buf, 0, result.Count);
            var node = JsonNode.Parse(msg);
            var type = (string?)node?["Type"];
            if (type != null)
                switch (type)
                {
                    case "host":
                    {
                        var data = JsonSerializer.Deserialize<Host>(msg);

                        if (data != null) roomId = manager.CreateRoom(data.Title, ws);

                        break;
                    }
                    case "signal":
                    {
                        var data = JsonSerializer.Deserialize<RtcSignal>(msg);

                        if (data != null && manager.Contains(data.SocketId))
                        {
                            var signalData = JsonSerializer.Serialize(new RtcSignal
                            {
                                Type = "signal",
                                Signal = data.Signal,
                                SocketId = peerId
                            });
                            await manager.SendMessageAsync(roomId, data.SocketId, signalData);
                        }

                        break;
                    }
                    case "sendInit":
                    {
                        var data = JsonSerializer.Deserialize<SendInit>(msg);
                        if (data != null)
                        {
                            var initData = JsonSerializer.Serialize(new SendInit
                            {
                                Type = "sendInit",
                                SocketId = peerId
                            });
                            await manager.SendMessageAsync(roomId, data.SocketId, initData);
                        }

                        break;
                    }
                }
        }
        else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
        {
            Debug.WriteLine($"Removing socket {peerId}");
            await manager.RemoveSocketAsync(roomId, peerId, result.CloseStatus, result.CloseStatusDescription);

            var removeData = JsonSerializer.Serialize(new RemovePeer
            {
                Type = "removePeer",
                SocketId = peerId
            });

            await manager.BroadcastAsync(roomId, removeData);
        }
    });
});

app.MapFallbackToFile("index.html");

await app.RunAsync();