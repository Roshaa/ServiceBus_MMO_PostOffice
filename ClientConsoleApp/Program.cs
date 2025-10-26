using Azure.Identity;
using Azure.Messaging.ServiceBus;
using DotNetEnv;
using ServiceBus_MMO_PostOffice.Messages.MessageTypes;
using SharedClasses.Contracts;
using SharedClasses.Messaging;



////// NOTES!! //////
///
/// I will not be saving data to a database here
/// This is just a client app that will receive the messages accordingly
/// The goal of this project is Service Bus with topics and subscriptions
///
/// Duplicate detection is not enabled on the topic in this demo.
/// It also looks easy to implement if needed.
/// 
/////////////////////



string HARDCODED_PLAYER_ID = "36";
List<RaidEvent> pendingRaids = new List<RaidEvent>();

Env.TraversePath().Load();

string sbNamespace = Environment.GetEnvironmentVariable("ServiceBus__Namespace") ?? throw new InvalidOperationException("Missing ServiceBus__Namespace in .env");
string topic = Environment.GetEnvironmentVariable("ServiceBus__Topic") ?? throw new InvalidOperationException("Missing ServiceBus__Topic in .env");
ServiceBusClient client = new ServiceBusClient(sbNamespace, new DefaultAzureCredential());



///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region PlayerCreated
string playerCreatedSub = PlayerCreatedSubscription.SubscriptionName;

ServiceBusSessionProcessor PlayerWelcomeProcessor = client.CreateSessionProcessor(
    topic,
    playerCreatedSub,
    new ServiceBusSessionProcessorOptions
    {
        SessionIds = { HARDCODED_PLAYER_ID },
        AutoCompleteMessages = false,
        MaxConcurrentSessions = 1,
        MaxConcurrentCallsPerSession = 1
    });

PlayerWelcomeProcessor.ProcessMessageAsync += async args =>
{
    PlayerCreated playerCreatedEvent = args.Message.Body.ToObjectFromJson<PlayerCreated>();
    Console.WriteLine($"{playerCreatedEvent.WelcomeMessage}");
    await args.CompleteMessageAsync(args.Message);
};

PlayerWelcomeProcessor.ProcessErrorAsync += args =>
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
};
#endregion
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region RaidEvents

string raidEventsSub = RaidEventsSubscription.SubscriptionName;

ServiceBusSessionProcessor RaidEventsProcessor = client.CreateSessionProcessor(
    topic,
    raidEventsSub,
    new ServiceBusSessionProcessorOptions
    {
        SessionIds = { HARDCODED_PLAYER_ID },
        AutoCompleteMessages = false,
        MaxConcurrentSessions = 1,
        MaxConcurrentCallsPerSession = 1,
        PrefetchCount = 10,
        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
    });

RaidEventsProcessor.ProcessMessageAsync += async args =>
{
    string subject = args.Message.Subject ?? (args.Message.ApplicationProperties.TryGetValue("type", out var t) ? t?.ToString() : null);

    switch (subject)
    {
        case RaidEventsSubscription.RaidInviteSubject:
            {
                RaidEvent ev = args.Message.Body.ToObjectFromJson<RaidEvent>();

                //Lets simulate a dead letter if a player logged in after the raid started
                if (DateTime.UtcNow > ev.EndTime)
                {
                    await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "RaidHasAlreadyEnded",
                    deadLetterErrorDescription: $"CurrentTime: {DateTime.UtcNow}, EndTime: {ev.EndTime}");

                    Console.WriteLine($"Dead lettered raid invite {ev.Id} because the raid has already ended.");
                    return;
                }

                Console.WriteLine($"{ev.Message}");
                Console.WriteLine($"Raid Details: StartTime={ev.StartTime:u}, EndTime={ev.EndTime:u}");

                //Here we would normally ask the player to accept or decline the raid invite and save to the server via an API call

                pendingRaids.Add(ev);
                break;
            }
        case RaidEventsSubscription.RaidCancelledSubject:
            {
                RaidEvent ev = args.Message.Body.ToObjectFromJson<RaidEvent>();
                Console.WriteLine($"{ev.Message}");
                if (pendingRaids.RemoveAll(r => r.Id == ev.Id) > 0)
                {
                    Console.WriteLine($"Removed raid {ev.Id} from pending raids.");
                }
                else
                {
                    Console.WriteLine($"Raid {ev.Id} was not found in pending raids.");
                }
                break;
            }
        case RaidEventsSubscription.RaidReminderSubject:
            {
                RaidReminder ev = args.Message.Body.ToObjectFromJson<RaidReminder>();

                //Lets simulate a dead letter if a player logged in after the raid started
                if (DateTime.UtcNow > ev.StartTime)
                {
                    await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "RaidHasAlreadyEnded",
                    deadLetterErrorDescription: $"CurrentTime: {DateTime.UtcNow}, StartTime: {ev.StartTime}");

                    Console.WriteLine($"Dead lettered raid invite {ev.RaidId} because the raid has already ended.");
                    return;
                }

                Console.WriteLine($"Your raid with Id={ev.RaidId} beginning at {ev.StartTime:u} is starting soon! Reminder Message: {ev.Message}");
                break;

            }
        default:
            await args.DeadLetterMessageAsync(args.Message, "UnknownSubject", subject);
            return;
    }

    await args.CompleteMessageAsync(args.Message);
};

RaidEventsProcessor.ProcessErrorAsync += args =>
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
};

#endregion
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region Maintenance (peek-only)
//since the message is the same for all players we can use a non-destructive reader to peek at the messages without locking or removing them from the subscription
//This is useful for global announcements that all players should see

string maintenanceSub = MaintenanceSubscription.SubscriptionName;

ServiceBusReceiver MaintenancePeek = client.CreateReceiver(topic, maintenanceSub);

long lastSeq = 0;

var maintenancePeekTask = Task.Run(async () =>
{
    while (true)
    {
        var batch = await MaintenancePeek.PeekMessagesAsync(
            maxMessages: 50,
            fromSequenceNumber: lastSeq == 0 ? (long?)null : lastSeq + 1);

        if (batch.Count == 0)
        {
            await Task.Delay(2000);
            continue;
        }

        foreach (var m in batch)
        {
            var notice = m.Body.ToObjectFromJson<ScheduledMaintenance>();
            Console.WriteLine($"[MAINT-PEEK] {notice.Message} Starts: {notice.MaintenanceStartTime:u} (seq={m.SequenceNumber})");
            lastSeq = m.SequenceNumber;
        }
    }
});

#endregion
////////////////////////////////////////////////////////////////////////////////////////////////




///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#region RaidEvents DLQ

ServiceBusProcessor RaidEventsDlqProcessor = client.CreateProcessor(
    topic,
    raidEventsSub,
    new ServiceBusProcessorOptions
    {
        SubQueue = SubQueue.DeadLetter,
        AutoCompleteMessages = false,
        MaxConcurrentCalls = 1,
        PrefetchCount = 10,
        MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
    });

RaidEventsDlqProcessor.ProcessMessageAsync += async args =>
{
    var messageArgs = args.Message;

    Console.WriteLine($"[DLQ] SessionId={messageArgs.SessionId} | Subject={messageArgs.Subject} | CorrelationId={messageArgs.CorrelationId}");
    Console.WriteLine($"[DLQ] Reason={messageArgs.DeadLetterReason} | Description={messageArgs.DeadLetterErrorDescription}");

    try
    {
        var ev = messageArgs.Body.ToObjectFromJson<RaidEvent>();
        Console.WriteLine($"[DLQ] RaidId={ev.Id} | Start={ev.StartTime:u} | End={ev.EndTime:u} | Msg={ev.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DLQ] parse failed: {ex.Message}");
    }

    await args.CompleteMessageAsync(messageArgs);
};

RaidEventsDlqProcessor.ProcessErrorAsync += args =>
{
    Console.WriteLine($"[DLQ][ERR] {args.Exception}");
    return Task.CompletedTask;
};

#endregion
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



await PlayerWelcomeProcessor.StartProcessingAsync();
Console.WriteLine($"Listening on {topic}/{playerCreatedSub} session={HARDCODED_PLAYER_ID} in {sbNamespace}. Press Ctrl+C to exit.");

await RaidEventsProcessor.StartProcessingAsync();
Console.WriteLine($"Listening on {topic}/{raidEventsSub} session={HARDCODED_PLAYER_ID} in {sbNamespace}. Press Ctrl+C to exit.");

await RaidEventsDlqProcessor.StartProcessingAsync();
Console.WriteLine($"Listening DLQ on {topic}/{raidEventsSub} in {sbNamespace}.");


Console.WriteLine("Press 'R' to list pending raids.");
Console.WriteLine("-----------------------------------------------------------------------------------------------");

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += async (_, e) =>
{
    e.Cancel = true;
    await PlayerWelcomeProcessor.StopProcessingAsync();
    await PlayerWelcomeProcessor.DisposeAsync();
    await RaidEventsProcessor.StopProcessingAsync();
    await RaidEventsProcessor.DisposeAsync();
    await RaidEventsDlqProcessor.StopProcessingAsync();
    await RaidEventsDlqProcessor.DisposeAsync();
    await client.DisposeAsync();
    tcs.TrySetResult();
};

var keyListener = Task.Run(async () =>
{
    while (!tcs.Task.IsCompleted)
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.R)
            {
                PrintPendingRaids();
            }
        }
        await Task.Delay(50);
    }
});

await tcs.Task;
await keyListener;

void PrintPendingRaids()
{
    foreach (var raid in pendingRaids)
        Console.WriteLine($"Pending Raid: Id={raid.Id}, StartTime={raid.StartTime:u}, EndTime={raid.EndTime:u}, Message={raid.Message}");
}