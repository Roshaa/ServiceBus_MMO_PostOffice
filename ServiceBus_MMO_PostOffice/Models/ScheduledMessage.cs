namespace ServiceBus_MMO_PostOffice.Models
{
    public class ScheduledMessage
    {
        public long Id { get; set; }
        public int RaidId { get; set; }
        public int PlayerId { get; set; }
        public string Subject { get; set; } = default!;
        public string? SessionId { get; set; }
        public DateTime ScheduledAtUtc { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
