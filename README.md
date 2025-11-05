Azure Service Bus Messaging Demo — “MMO Post Office”

.NET 8, Azure Service Bus (Topics/Subscriptions/Sessions), EF Core, AutoMapper, DefaultAzureCredential

Built a topic-based event architecture with CorrelationRuleFilter(Subject) and code-first subscription/rule bootstrap. Removed the default rule to guarantee only intended events flow.

Implemented per-user fan-out with sessions (SessionId = playerId). Wrote session processors with prefetch, max concurrent calls/sessions, and auto lock renewal; routed by Subject & app properties; published with message batching for throughput.

Made consumers robust and self-healing: applied TTL, dead-lettering (unknown subject / stale invite) with a DLQ processor that inspects reason/description and either quarantines or replays messages marked as transient failures.

Demonstrated scheduling two ways:

Broker scheduling (ScheduleMessagesAsync/CancelScheduledMessagesAsync) for broadcast maintenance notices.

Application-level scheduling via an hourly BackgroundService that emits per-player raid reminders (1h prior) from a simple DB table—illustrating trade-offs between broker durability and app-level control.

Designed for idempotency & tracing: consistent MessageId, CorrelationId (Activity trace), and ApplicationProperties["playerId"]; topic-level duplicate detection is off in the demo but the pipeline is ready for it.

Clean developer experience: Swagger for the API, DefaultAzureCredential with connection-string fallback, lean EF models/DTOs via AutoMapper.

What this shows quickly: sessions (ordering + affinity), batching, TTL, DLQ handling, correlation filters, broker vs. app scheduling, and practical consumer options (peek-only for broadcast).

Keywords: Azure Service Bus, Topics/Subscriptions, Sessions, Correlation filters, DLQ, TTL, Scheduled messages, Batching, Prefetch, Lock renewal, Idempotency, DefaultAzureCredential, .NET 8.
