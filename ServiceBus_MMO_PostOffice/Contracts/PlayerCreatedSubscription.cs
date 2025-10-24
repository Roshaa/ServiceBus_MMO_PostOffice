namespace SharedClasses.Contracts
{
    public static class PlayerCreatedSubscription
    {
        //should be defined in appsettings but i will keep it here

        public const string SubscriptionName = "player-created-welcome";
        public const string RuleName = "player-created-filter";
        public const string Subject = "PlayerCreated";
    }
}
