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

            ScheduledMessage[] scheduledMessages = await GetScheduledMessages(db, ct);

            while (scheduledMessages.Length > 0)
            {
                List<ServiceBusMessage> messagesToPublish = new List<ServiceBusMessage>();

                int[] raidIds = scheduledMessages
                    .Select(sm => sm.RaidId)
                    .Distinct()
                    .ToArray();

                var raidStartTimes = await db.Raid
                        .AsNoTracking()
                        .Where(r => raidIds.Contains(r.Id))
                        .Select(r => new { r.Id, r.StartTime })
                        .ToDictionaryAsync(x => x.Id, x => x.StartTime, ct);

                foreach (ScheduledMessage message in scheduledMessages)
                {
                    DateTime raidStartTime = raidStartTimes.FirstOrDefault(rt => rt.Key == message.RaidId).Value;

                    if (raidStartTime == default)
                    {
                        _log.LogWarning("ScheduledMessage {ScheduledMessageId} references non-existent Raid {RaidId}", message.Id, message.RaidId);
                        db.ScheduledMessage.Remove(message);
                        continue;
                    }

                    var ttl = raidStartTime - DateTime.UtcNow;
                    if (ttl <= TimeSpan.Zero)
                    {
                        _log.LogWarning("ScheduledMessage {ScheduledMessageId} for Raid {RaidId} has non-positive TTL {TTL}", message.Id, message.RaidId, ttl);
                        db.ScheduledMessage.Remove(message);
                        continue;
                    }

                    RaidReminder raidReminder = new RaidReminder
                    {
                        RaidId = message.RaidId,
                        StartTime = raidStartTime,
                        Message = $"Reminder: Your raid {message.RaidId} is scheduled at {raidStartTime:u} UTC."
                    };

                    messagesToPublish.Add(_publisher.CreateMessage<RaidReminder>(
                        raidReminder,
                        RaidEventsSubscription.RaidReminderSubject,
                        ttl: ttl,
                        sessionId: message.PlayerId.ToString()
                    ));

                    db.ScheduledMessage.Remove(message);
                }

                if (messagesToPublish.Count > 0)
                    await _publisher.PublishBatchAsync(messagesToPublish);

                await db.SaveChangesAsync(ct);

                scheduledMessages = await GetScheduledMessages(db, ct);
            }
        }

        private async Task<ScheduledMessage[]> GetScheduledMessages(ApplicationDbContext db, CancellationToken ct)
        {
            DateTime currentTimeMinusOneHour = DateTime.UtcNow - TimeSpan.FromHours(1);
            int pageSize = 500;

            ScheduledMessage[] scheduledMessages = await db.ScheduledMessage
                .Where(sm => sm.ScheduledAtUtc <= currentTimeMinusOneHour)
                .OrderBy(sm => sm.ScheduledAtUtc)
                .Take(pageSize)
                .ToArrayAsync(ct);

            return scheduledMessages;
        }
    }
}