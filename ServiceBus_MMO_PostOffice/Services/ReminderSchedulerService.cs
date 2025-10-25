using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using ServiceBus_MMO_PostOffice.Data;
using ServiceBus_MMO_PostOffice.Models;
using SharedClasses.Contracts;
using SharedClasses.Messaging;

namespace ServiceBus_MMO_PostOffice.Services
{
    public sealed class ReminderSchedulerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderSchedulerService> _log;
        private readonly PostOfficeServiceBusPublisher _publisher;

        public ReminderSchedulerService(IServiceScopeFactory scopeFactory, ILogger<ReminderSchedulerService> log, PostOfficeServiceBusPublisher publisher)
        {
            _scopeFactory = scopeFactory;
            _log = log;
            _publisher = publisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            await DoStuff(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { await DoStuff(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex) { _log.LogError(ex, "ReminderScheduler tick failed"); }
            }
        }

        private async Task DoStuff(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var currentTimeMinusOneHour = DateTime.UtcNow - TimeSpan.FromHours(1);

            ScheduledMessage[] scheduledMessages = await db.ScheduledMessage
                .Where(sm => sm.ScheduledAtUtc <= currentTimeMinusOneHour)
                .ToArrayAsync(ct);

            List<ServiceBusMessage> messagesToPublish = new List<ServiceBusMessage>();

            foreach (ScheduledMessage message in scheduledMessages)
            {
                Raid raid = await db.Raid.FindAsync(message.RaidId);
                if (raid is null)
                {
                    _log.LogWarning("ScheduledMessage {ScheduledMessageId} references non-existing Raid {RaidId}", message.Id, message.RaidId);
                    continue;
                }

                RaidReminder raidReminder = new RaidReminder
                {
                    RaidId = message.RaidId,
                    StartTime = raid.StartTime,
                    Message = $"Reminder: Your raid {message.RaidId} is scheduled at {message.ScheduledAtUtc:u} UTC."
                };

                messagesToPublish.Add(_publisher.CreateMessage<RaidReminder>(
                    raidReminder,
                    RaidEventsSubscription.RaidReminderSubject,
                    ttl: raid.StartTime - DateTime.UtcNow,
                    sessionId: message.PlayerId.ToString()
                ));

                db.ScheduledMessage.Remove(message);
            }

            await _publisher.PublishBatchAsync(messagesToPublish);
            await db.SaveChangesAsync(ct);
        }
    }
}