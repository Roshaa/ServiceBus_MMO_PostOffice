namespace ServiceBus_MMO_PostOffice.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string NickName { get; set; } = string.Empty;
        public int? GuildId { get; set; }
        public Guild? Guild { get; set; }
    }
}
