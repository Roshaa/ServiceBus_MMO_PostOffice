# 📨 Azure Service Bus Messaging Demo — *“MMO Post Office”*

**Tech Stack:** .NET 8 · Azure Service Bus (Topics/Subscriptions/Sessions) · EF Core · AutoMapper · DefaultAzureCredential

A lightweight **event-driven** demo simulating an MMO’s post office. It showcases **topic pub/sub**, **session affinity**, **correlation filters**, **DLQ recovery**, **broker vs. app scheduling**, **batching**, **prefetch**, **auto lock renewal**, and **idempotency/tracing**.

---

## Highlights

- **Topic pub/sub + correlation filters**
  - Code-first bootstrap for topics, subscriptions, and rules.
  - **Default rule removed** → only intended events flow.
  - `CorrelationRuleFilter(Subject)` + App Properties for routing.

- **Per-player affinity with sessions**
  - `SessionId = playerId` → ordered, isolated streams per user.
  - Session processors tuned with **prefetch**, **max concurrent calls/sessions**, **auto lock renewal**.

- **Throughput & robustness**
  - **Message batching** for publish.
  - **TTL** + **dead-lettering** for stale/invalid messages.
  - **DLQ processor** inspects reason/description → **quarantine** or **replay** transient failures.

- **Two scheduling models**
  - **Broker:** `ScheduleMessagesAsync` / `CancelScheduledMessagesAsync` for broadcast maintenance.
  - **App:** hourly `BackgroundService` emits per-player raid reminders (T-1h) from a DB table.
  - Trade-offs: **broker durability** vs **app-level control**.

- **Idempotency & tracing**
  - Consistent `MessageId`, `CorrelationId` (Activity), and `ApplicationProperties["playerId"]`.
  - Topic duplicate detection off (demo), pipeline ready to enable.

- **DX**
  - Swagger for the API, clean EF models/DTOs with AutoMapper.
  - `DefaultAzureCredential` with connection-string fallback.
