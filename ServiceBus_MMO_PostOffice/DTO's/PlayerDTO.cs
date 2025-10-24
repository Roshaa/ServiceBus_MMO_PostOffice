namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class PlayerDTO
    {
        public int Id { get; set; }
        public string NickName { get; set; } = string.Empty;
        public GuildDTO Guild { get; set; }
    }
}
