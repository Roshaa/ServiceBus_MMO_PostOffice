namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class GuildDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
