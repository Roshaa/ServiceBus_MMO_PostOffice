using Azure.Identity;
using Azure.Messaging.ServiceBus;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.Mappers;
using ServiceBus_MMO_PostOffice.Services;

var builder = WebApplication.CreateBuilder(args);

Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();
var cfg = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

var cs = cfg.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing.");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(cs));

builder.Services.AddSingleton(sp =>
{
    var conn = cfg["ServiceBus:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(conn)) return new ServiceBusClient(conn, new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
    var ns = cfg["ServiceBus:Namespace"] ?? throw new InvalidOperationException("ServiceBus:Namespace missing.");
    var fqns = ns.Contains(".servicebus.windows.net", StringComparison.OrdinalIgnoreCase) ? ns : $"{ns}.servicebus.windows.net";
    return new ServiceBusClient(fqns, new DefaultAzureCredential(), new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
});

builder.Services.AddSingleton(sp =>
{
    var topic = cfg["ServiceBus:Topic"] ?? throw new InvalidOperationException("ServiceBus:Topic missing.");
    return sp.GetRequiredService<ServiceBusClient>().CreateSender(topic);
});

builder.Services.AddSingleton<PostOfficeServiceBusPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    new DataSeeder().Seed(db);

    app.MapOpenApi();
    app.UseSwaggerUI(o => { o.SwaggerEndpoint("/openapi/v1.json", "ServiceBus_MMO_PostOffice v1"); o.RoutePrefix = "swagger"; });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
