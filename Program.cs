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

    // TODO: switch to state machine pattern
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
                        if (roomId != null || peerId != null)
                        {
                            Debug.WriteLine($"{peerId} tried to host more than one room");
                            break;
                        }

                        var data = JsonSerializer.Deserialize<Host>(msg);
                        if (data != null)
                        {
                            var success = manager.CreateRoom(data.Title, data.Public, ws, out roomId, out peerId);

                            if (success)
                                await manager.SendMessageAsync(
                                    roomId,
                                    peerId,
                                    JsonSerializer.Serialize(new Connected { Type = "connected", SocketId = peerId })
                                );
                        }

                        break;
                    }
                    case "join":
                    {
                        if (roomId != null || peerId != null)
                        {
                            Debug.WriteLine($"{peerId} tried to join more than one room");
                            break;
                        }

                        var data = JsonSerializer.Deserialize<Join>(msg);

                        if (data != null)
                        {
                            var success = manager.AddPeer(data.RoomId, ws, out peerId);

                            if (!success)
                            {
                                await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                                    "Provided room ID does not exist",
                                    CancellationToken.None);
                                return;
                            }

                            await manager.SendMessageAsync(
                                data.RoomId,
                                peerId,
                                JsonSerializer.Serialize(new Connected { Type = "connected", SocketId = peerId })
                            );

                            await manager.BroadcastAsync(
                                data.RoomId,
                                JsonSerializer.Serialize(new ReceiveInit { Type = "receiveInit", SocketId = peerId }),
                                ws
                            );
                        }

                        break;
                    }
                    case "publicRooms":
                    {
                        var data = JsonSerializer.Serialize(new PublicRooms
                        {
                            Type = "publicRooms",
                            Rooms = manager.GetPublicRooms()
                        });

                        var buffer = Encoding.UTF8.GetBytes(data);
                        var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                        await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                        break;
                    }
                    case "signal":
                    {
                        if (roomId == null || peerId == null)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Not hosting or joined server",
                                CancellationToken.None);
                            return;
                        }

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
                        if (roomId == null || peerId == null)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Not hosting or joined server",
                                CancellationToken.None);
                            return;
                        }

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

            if (roomId == null || peerId == null)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed idk",
                    CancellationToken.None);
                return;
            }

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