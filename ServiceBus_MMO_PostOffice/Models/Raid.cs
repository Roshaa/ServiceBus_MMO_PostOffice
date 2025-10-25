namespace ServiceBus_MMO_PostOffice.Models
{
    public class Raid
    {
        public int Id { get; set; }
        public int GuildId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }


        public Guild Guild { get; set; }
        public ICollection<RaidParticipant> RaidParticipant { get; set; } = [];
    }
}
