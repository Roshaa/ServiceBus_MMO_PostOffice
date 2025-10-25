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



await PlayerWelcomeProcessor.StartProcessingAsync();
Console.WriteLine($"Listening on {topic}/{playerCreatedSub} session={HARDCODED_PLAYER_ID} in {sbNamespace}. Press Ctrl+C to exit.");

await RaidEventsProcessor.StartProcessingAsync();
Console.WriteLine($"Listening on {topic}/{raidEventsSub} session={HARDCODED_PLAYER_ID} in {sbNamespace}. Press Ctrl+C to exit.");

Console.WriteLine("Press 'R' to list pending raids.");
Console.WriteLine("-----------------------------------------------------------------------------------------------");

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += async (_, e) =>
{
    e.Cancel = true;
    await PlayerWelcomeProcessor.StopProcessingAsync();
    await RaidEventsProcessor.StopProcessingAsync();
    await PlayerWelcomeProcessor.DisposeAsync();
    await RaidEventsProcessor.DisposeAsync();
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