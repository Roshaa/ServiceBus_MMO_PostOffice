using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ServiceBus_MMO_PostOffice.Azure.Integration
{
    public static class ServiceBusBootstrap
    {
        public static async Task EnsureSubscriptionAsync(
         ServiceBusAdministrationClient admin,
         string topic,
         string subscription,
         bool requiresSessions,
         IEnumerable<CreateRuleOptions> desiredRules,
         TimeSpan? autoDeleteOnIdle = null)
        {
            if (!await admin.SubscriptionExistsAsync(topic, subscription))
            {
                var opts = new CreateSubscriptionOptions(topic, subscription)
                {
                    RequiresSession = requiresSessions,
                    AutoDeleteOnIdle = autoDeleteOnIdle ?? TimeSpan.MaxValue
                };
                await admin.CreateSubscriptionAsync(opts);
            }

            var existing = new List<RuleProperties>();
            await foreach (var r in admin.GetRulesAsync(topic, subscription))
                existing.Add(r);

            var desiredNames = new HashSet<string>(desiredRules.Select(r => r.Name),
                                                   StringComparer.OrdinalIgnoreCase);

            foreach (var rule in desiredRules)
            {
                var match = existing.FirstOrDefault(x => x.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    await admin.DeleteRuleAsync(topic, subscription, match.Name);

                await admin.CreateRuleAsync(topic, subscription, rule);
            }

            foreach (var r in existing)
            {
                if (!desiredNames.Contains(r.Name) &&
                    !r.Name.Equals(RuleProperties.DefaultRuleName, StringComparison.Ordinal))
                {
                    await admin.DeleteRuleAsync(topic, subscription, r.Name);
                }
            }

            bool hasNonDefault = false;
            bool defaultExists = false;

            await foreach (var r in admin.GetRulesAsync(topic, subscription))
            {
                if (r.Name.Equals(RuleProperties.DefaultRuleName, StringComparison.Ordinal))
                    defaultExists = true;
                else
                    hasNonDefault = true;
            }

            if (hasNonDefault && defaultExists)
            {
                try
                {
                    await admin.DeleteRuleAsync(topic, subscription, RuleProperties.DefaultRuleName);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    //Ignore
                }
            }
        }

    }
}