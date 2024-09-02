using PuzzAPI.Data.Contexts;

namespace PuzzAPI.Setup;

public static class ApplicationExtensions
{
    public static WebApplication ConfigureDatabase(this WebApplication app)
    {
        // create database if not already created
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<UserContext>();
            context.Database.EnsureCreated();
        }

        return app;
    }

    public static WebApplication ConfigureMiddleWare(this WebApplication app)
    {
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(50)
        });
        app.UseStaticFiles();
        app.UseAuthentication();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(ServiceExtensions.AllowLocalHostCorsRule);
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();

        return app;
    }
}