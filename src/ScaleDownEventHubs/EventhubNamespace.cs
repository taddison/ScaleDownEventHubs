namespace ScaleDownEventHubs
{
    internal class EventhubNamespace
    {
        public EventhubNamespace(string subscriptionId, string resourceGroup, string @namespace, int capacity)
        {
            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            Namespace = @namespace;
            Capacity = capacity;
        }

        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string Namespace { get; set; }
        public int Capacity { get; set; }
    }
}
