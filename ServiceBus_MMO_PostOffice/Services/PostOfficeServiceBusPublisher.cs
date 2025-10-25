using Azure.Messaging.ServiceBus;
using ServiceBus_MMO_PostOffice.Messages.MessageTypes;
using System.Diagnostics;

namespace ServiceBus_MMO_PostOffice.Services
{
    public class PostOfficeServiceBusPublisher : IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<PostOfficeServiceBusPublisher> _log;
        private const string JsonContentType = "application/json";


        public PostOfficeServiceBusPublisher(ServiceBusSender sender, ILogger<PostOfficeServiceBusPublisher> log)
        {
            _sender = sender;
            _log = log;
        }

        public ServiceBusMessage CreateMessage<T>(T payload, string subject, TimeSpan? ttl, string sessionId = null)
        {
            var msg = new ServiceBusMessage(BinaryData.FromObjectAsJson(payload))
            {
                ContentType = JsonContentType,
                Subject = subject,
                CorrelationId = Activity.Current?.TraceId.ToString(),
                MessageId = $"{subject}:{Guid.NewGuid():N}"
            };

            if (!string.IsNullOrWhiteSpace(sessionId)) msg.SessionId = sessionId;

            if (ttl.HasValue)
            {
                if (ttl < TimeSpan.Zero) ttl = TimeSpan.FromMinutes(3); //This is for testing a deadletter queue i made.
                msg.TimeToLive = ttl.Value;
            }

            var playerId = payload switch { PlayerCreated p => p.Id, _ => 0 };
            if (playerId > 0) msg.ApplicationProperties["playerId"] = playerId;

            return msg;
        }

        public async Task PublishMessageAsync(ServiceBusMessage message, CancellationToken ct = default)
        {
            await _sender.SendMessageAsync(message, ct);
        }

        public async Task PublishBatchAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken ct = default)
        {
            var batch = await _sender.CreateMessageBatchAsync(ct);
            try
            {
                foreach (var m in messages)
                {
                    if (!batch.TryAddMessage(m))
                    {
                        await _sender.SendMessagesAsync(batch, ct);
                        batch.Dispose();
                        batch = await _sender.CreateMessageBatchAsync(ct);

                        if (!batch.TryAddMessage(m)) throw new InvalidOperationException("Single message too large for an empty batch.");
                    }
                }

                if (batch.Count > 0)
                    await _sender.SendMessagesAsync(batch, ct);
            }
            finally
            {
                batch.Dispose();
            }
        }

        public async ValueTask DisposeAsync() => await _sender.DisposeAsync();

    }
}
