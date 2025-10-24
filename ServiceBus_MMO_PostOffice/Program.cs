using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.Mappers;
using ServiceBus_MMO_PostOffice.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

builder.Services.AddSingleton(sp =>
{
    var ns = configuration["ServiceBus:Namespace"];
    var fqns = $"{ns}.servicebus.windows.net";
    return new ServiceBusClient(
        fqns,
        new Azure.Identity.DefaultAzureCredential(),
        new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var entity = configuration["ServiceBus:Entity"];
    return client.CreateSender(entity);
});

builder.Services.AddSingleton<PostOfficeServiceBusPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        new DataSeeder().Seed(db);
    }

    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ServiceBus_MMO_PostOffice v1");
        options.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
