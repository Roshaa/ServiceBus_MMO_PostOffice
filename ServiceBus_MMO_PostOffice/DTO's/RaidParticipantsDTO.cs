namespace ServiceBus_MMO_PostOffice.DTO_s
{
    public sealed record class RaidParticipantsDTO
    {
        public int Id { get; set; }
        public string NickName { get; set; }
        public bool InviteAccepted { get; set; }
    }
}
