namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class GuildRelationDTO
    {
        public int GuildId { get; init; }
        public int PlayerId { get; init; }
    }
}
