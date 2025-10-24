using Azure.Messaging.ServiceBus;
using ServiceBus_MMO_PostOffice.Messages;
using System.Diagnostics;

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

        public async Task<ServiceBusMessage> CreateMessageAsync<T>(GenericMessage<T> m)
        {
            ServiceBusMessage msg = new ServiceBusMessage(BinaryData.FromObjectAsJson(m.Payload))
            {
                ContentType = "application/json",
                Subject = m.Subject,
                CorrelationId = Activity.Current?.TraceId.ToString()
            };

            if (m.TimeToLive.HasValue) msg.TimeToLive = m.TimeToLive.Value;

            if (!string.IsNullOrWhiteSpace(m.PlayerId)) msg.ApplicationProperties["PlayerId"] = m.PlayerId;
            if (!string.IsNullOrWhiteSpace(m.GuildId)) msg.ApplicationProperties["GuildId"] = m.GuildId;

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
