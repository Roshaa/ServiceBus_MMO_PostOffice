using Azure.Messaging.ServiceBus;

namespace ServiceBus_MMO_PostOffice.Services
{
    public class PostOfficeServiceBusPublisher : IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<PostOfficeServiceBusPublisher> _log;

        public PostOfficeServiceBusPublisher(ServiceBusSender sender, ILogger<PostOfficeServiceBusPublisher> log)
        {
            _sender = sender;
            _log = log;
        }

        public async Task PublishAsync<T>(T payload, string? subject = null, string? correlationId = null, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            var msg = new ServiceBusMessage(BinaryData.FromObjectAsJson(payload))
            {
                ContentType = "application/json",
                Subject = subject,
                CorrelationId = correlationId
            };
            if (ttl.HasValue) msg.TimeToLive = ttl.Value;

            // Optional: deterministic id for duplicate detection (Standard/Premium)
            // msg.MessageId = $"{subject}:{correlationId}:{SomeKey}";

            await _sender.SendMessageAsync(msg, ct);
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

                        if (!batch.TryAddMessage(m))
                            throw new InvalidOperationException("Single message too large for an empty batch.");
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
