using System.Net;
using PuzzAPI.ConnectionHandler;
using PuzzAPI.RoomManager;

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

    var handler = new ConnectionHandler(ws, manager);

    await handler.Run();
});

app.MapFallbackToFile("index.html");

await app.RunAsync();