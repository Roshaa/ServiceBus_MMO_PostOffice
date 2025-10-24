namespace ServiceBus_MMO_PostOffice.Messages
{
    public class GenericMessage<T>
    {
        public T Payload { get; set; }
        public string Subject { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public string PlayerId { get; set; }
        public string GuildId { get; set; }
    }
}
