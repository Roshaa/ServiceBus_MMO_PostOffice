using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Bootstrap;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.Mappers;
using ServiceBus_MMO_PostOffice.Services;
using SharedClasses.Contracts;

var builder = WebApplication.CreateBuilder(args);

Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();
var cfg = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

var cs = cfg.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing.");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(cs));

var ns = cfg["ServiceBus:Namespace"] ?? throw new InvalidOperationException("ServiceBus:Namespace missing.");

builder.Services.AddSingleton(sp =>
{
    var conn = cfg["ServiceBus:ConnectionString"];

    if (!string.IsNullOrWhiteSpace(conn))
        return new ServiceBusClient(conn, new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });

    return new ServiceBusClient(ns, new DefaultAzureCredential(), new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
});

builder.Services.AddSingleton(sp =>
{
    var topic = cfg["ServiceBus:Topic"];
    return sp.GetRequiredService<ServiceBusClient>().CreateSender(topic);
});

ServiceBusAdministrationClient admin =
    !string.IsNullOrWhiteSpace(cfg["ServiceBus:ConnectionString"])
    ? new ServiceBusAdministrationClient(cfg["ServiceBus:ConnectionString"])
    : new ServiceBusAdministrationClient(ns, new DefaultAzureCredential());

builder.Services.AddSingleton<PostOfficeServiceBusPublisher>();
builder.Services.AddHostedService<ReminderSchedulerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //I Dont know IaC yet :)

    await ServiceBusBootstrap.EnsureSubscriptionAsync(
    admin,
    topic: cfg["ServiceBus:Topic"],
    subscription: PlayerCreatedSubscription.SubscriptionName,
    requiresSessions: true,
    desiredRules: new List<CreateRuleOptions>
    {
        new CreateRuleOptions(PlayerCreatedSubscription.RuleName,
        new CorrelationRuleFilter { Subject = PlayerCreatedSubscription.Subject })
    });

    await ServiceBusBootstrap.EnsureSubscriptionAsync(
    admin,
    topic: cfg["ServiceBus:Topic"],
    subscription: RaidEventsSubscription.SubscriptionName,
    requiresSessions: true,
    desiredRules: new[]
    {
        new CreateRuleOptions(RaidEventsSubscription.InviteRuleName,
        new CorrelationRuleFilter { Subject = RaidEventsSubscription.RaidInviteSubject }),
        new CreateRuleOptions(RaidEventsSubscription.CancelRuleName,
        new CorrelationRuleFilter { Subject = RaidEventsSubscription.RaidCancelledSubject }),
        new CreateRuleOptions(RaidEventsSubscription.ReminderRuleName,
        new CorrelationRuleFilter { Subject = RaidEventsSubscription.RaidReminderSubject })
    },
    autoDeleteOnIdle: TimeSpan.FromDays(31));

    await ServiceBusBootstrap.EnsureSubscriptionAsync(
    admin,
    topic: cfg["ServiceBus:Topic"],
    subscription: MaintenanceSubscription.SubscriptionName,
    requiresSessions: false,
    desiredRules: new[]
    {
        new CreateRuleOptions(MaintenanceSubscription.RuleName,
            new CorrelationRuleFilter { Subject = MaintenanceSubscription.Subject })
    });


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
