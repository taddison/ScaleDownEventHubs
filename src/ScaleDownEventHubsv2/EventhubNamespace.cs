namespace ScaleDownEventHubsv2
{
    internal class EventhubNamespace
    {
        public EventhubNamespace(string subscriptionId, string resourceGroup, string @namespace, int targetThroughputUnits)
        {
            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            Namespace = @namespace;
            TargetThroughputUnits = targetThroughputUnits;
        }

        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string Namespace { get; set; }
        public int TargetThroughputUnits { get; set; }
    }
}
