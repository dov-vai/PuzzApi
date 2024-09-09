using PuzzAPI.Endpoints;
using PuzzAPI.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureCors();
builder.Services.ConfigureServices(builder.Configuration);
builder.Services.SetupJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.ConfigureDatabase();
app.ConfigureMiddleWare();

app.MapWebSocketEndpoint()
    .MapAuthEndpoints()
    .MapInfoEndpoints();

app.MapFallbackToFile("index.html");

await app.RunAsync();