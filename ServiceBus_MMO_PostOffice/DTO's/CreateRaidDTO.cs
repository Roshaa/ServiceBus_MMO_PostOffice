namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class CreateRaidDTO
    {
        public int GuildId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
