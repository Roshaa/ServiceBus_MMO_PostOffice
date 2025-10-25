namespace ServiceBus_MMO_PostOffice.Models
{
    public class RaidParticipant
    {
        public int Id { get; set; }
        public int RaidId { get; set; }
        public int PlayerId { get; set; }

        public Raid Raid { get; set; }
        public Player Player { get; set; }
    }
}
