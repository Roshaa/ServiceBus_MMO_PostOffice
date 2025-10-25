namespace SharedClasses.Messaging
{
    public class RaidEvent
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
