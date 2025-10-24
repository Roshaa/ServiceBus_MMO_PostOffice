namespace ServiceBus_MMO_PostOffice.Messages.MessageTypes
{
    public class PlayerCreated
    {
        public int Id { get; set; }
        public string NickName { get; set; }
        public string WelcomeMessage { get; } = "Welcome to the MMO Post Office!";
        public DateTime CreatedAt { get; } = DateTime.Now;
    }
}
