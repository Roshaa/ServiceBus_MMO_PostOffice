namespace SharedClasses.Contracts
{
    public static class RaidEventsSubscription
    {
        public const string SubscriptionName = "raid-events";
        public const string InviteRuleName = "raid-invite-filter";
        public const string CancelRuleName = "raid-cancelled-filter";
        public const string ReminderRuleName = "raid-reminder-filter";
        public const string RaidInviteSubject = "RaidInvite";
        public const string RaidCancelledSubject = "RaidCancelled";
        public const string RaidReminderSubject = "RaidReminder";
    }
}
