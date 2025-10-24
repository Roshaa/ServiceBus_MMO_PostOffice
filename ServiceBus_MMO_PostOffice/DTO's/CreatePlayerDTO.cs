namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class CreatePlayerDTO
    {
        public string NickName { get; set; } = string.Empty;
    }
}
