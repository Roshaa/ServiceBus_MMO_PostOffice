namespace ServiceBus_MMO_PostOffice.Messages.MessageTypes
{
    public class PlayerCreated
    {
        public string NickName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
