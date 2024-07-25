using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PuzzAPI.Types;
using PuzzAPI.WebSocketConnectionManager;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();

var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Map("/ws", async (HttpContext context, IWebSocketConnectionManager manager) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    if (context.Request.Query.ContainsKey("host"))
    {
        var success = bool.TryParse(context.Request.Query["host"], out var host);

        if (!success) return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();

    var id = manager.AddSocket(ws);

    await manager.SendMessageAsync(id, JsonSerializer.Serialize(new Connected { Type = "connected", SocketId = id }));

    // tell sockets to prepare a peer for connection
    var msg = JsonSerializer.Serialize(new ReceiveInit
    {
        Type = "receiveInit",
        SocketId = id
    });
    await manager.BroadcastAsync(msg, ws);
    //

    var curName = context.Request.Query["name"];

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
                    case "signal":
                    {
                        var data = JsonSerializer.Deserialize<RtcSignal>(msg);

                        if (data != null && manager.Contains(data.SocketId))
                        {
                            var signalData = JsonSerializer.Serialize(new RtcSignal
                            {
                                Type = "signal",
                                Signal = data.Signal,
                                SocketId = id
                            });
                            await manager.SendMessageAsync(data.SocketId, signalData);
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
                                SocketId = id
                            });
                            await manager.SendMessageAsync(data.SocketId, initData);
                        }

                        break;
                    }
                }
        }
        else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
        {
            Debug.WriteLine($"Removing socket {id}");
            await manager.RemoveSocketAsync(id, result.CloseStatus, result.CloseStatusDescription);

            var removeData = JsonSerializer.Serialize(new RemovePeer
            {
                Type = "removePeer",
                SocketId = id
            });

            await manager.BroadcastAsync(removeData);
        }
    });
});

app.MapFallbackToFile("index.html");

await app.RunAsync();