using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using ServiceBus_MMO_PostOffice.Services;
using SharedClasses.Contracts;
using SharedClasses.Messaging;

namespace ServiceBus_MMO_PostOffice.Controllers
{
    [ApiController]
    [Route("api/sendlater")]
    public class SendMessagesLaterController(ServiceBusSender sender, PostOfficeServiceBusPublisher publisher) : ControllerBase
    {
        //I havent worked with scheduled messages.

        //In MMOS you often have planned maintenance windows.
        //During these windows you want to notify players in advance.
        //So lets schedule global messages, no sessions then ttl auto expire them after the maintenance window.


        [HttpPost]
        public async Task<ActionResult> ScheduleMaintenance([FromBody] DateTime startTime, CancellationToken ct)
        {

            DateTime now = DateTime.UtcNow;
            if (startTime <= now.AddHours(1))
                return BadRequest("Maintenance must be at least 1 hour in the future (UTC).");

            ScheduledMaintenance maintenance = new ScheduledMaintenance
            {
                MaintenanceStartTime = startTime,
                Message = $"REMINDER: Planned maintenance at {startTime:u}."
            };

            TimeSpan ttl = (startTime - now) + TimeSpan.FromMinutes(1);
            if (ttl < TimeSpan.FromMinutes(1)) ttl = TimeSpan.FromMinutes(1);

            ServiceBusMessage msg60 = publisher.CreateMessage<ScheduledMaintenance>(maintenance, MaintenanceSubscription.Subject, ttl);
            ServiceBusMessage msg15 = publisher.CreateMessage<ScheduledMaintenance>(maintenance, MaintenanceSubscription.Subject, ttl);

            DateTimeOffset enqueue60 = new DateTimeOffset(startTime.AddMinutes(-60));
            DateTimeOffset enqueue15 = new DateTimeOffset(startTime.AddMinutes(-15));

            long seq60 = await sender.ScheduleMessageAsync(msg60, enqueue60, ct);
            long seq15 = await sender.ScheduleMessageAsync(msg15, enqueue15, ct);

            return Ok(new { startTime, scheduled = new[] { seq60, seq15 } });
        }

    }
}
