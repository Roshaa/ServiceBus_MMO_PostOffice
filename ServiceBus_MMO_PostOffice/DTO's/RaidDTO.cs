namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class RaidDTO
    {
        public int Id { get; set; }
        public int GuildId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<RaidParticipantsDTO> RaidParticipants { get; set; } = new();
    }
}
