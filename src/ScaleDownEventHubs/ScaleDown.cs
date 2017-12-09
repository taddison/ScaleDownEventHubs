using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Azure.Management.EventHub;
using Microsoft.Azure.Management.EventHub.Models;
using System.Collections.Generic;

namespace ScaleDownEventHubs
{
    public static class ScaleDown
    {
        [FunctionName("ScaleDown")]
        public static void Run([TimerTrigger("0 30 1 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"ScaleDown execution started");

            log.Info("Getting credentials");
            var creds = GetCredentials(log);

            var namespaces = new List<EventhubNamespace>
            {
                new EventhubNamespace("<subscription id>", "<resource group>", "<namespace>", 1)
            };

            foreach (var ns in namespaces)
            {
                log.Info($"Processing scaledown for {ns.Namespace} in {ns.ResourceGroup}");
                ScaleDownNamespace(ns, creds, log);
            }
            
            log.Info("ScaleDown execution completed");
        }

        private static void ScaleDownNamespace(EventhubNamespace ns, TokenCredentials credentials, TraceWriter log)
        {
            var ehClient = new EventHubManagementClient(credentials)
            {
                SubscriptionId = ns.SubscriptionId
            };

            log.Info("Querying namespace information");
            var nsInfo = ehClient.Namespaces.Get(ns.ResourceGroup, ns.Namespace);
            if (nsInfo.Sku.Capacity <= ns.Capacity)
            {
                log.Info($"Namespace {ns.Namespace} in {ns.ResourceGroup} already below target capacity (Current:{nsInfo.Sku.Capacity} Target:{ns.Capacity})");
                return;
            }

            var nsUpdate = new EHNamespace()
            {
                Sku = new Sku(nsInfo.Sku.Name, capacity: ns.Capacity)
            };

            log.Info($"Updating Namespace {ns.Namespace} in {ns.ResourceGroup} from {nsInfo.Sku.Capacity} to {nsUpdate.Sku.Capacity}");
            ehClient.Namespaces.Update(ns.ResourceGroup, ns.Namespace, nsUpdate);
        }

        private static TokenCredentials GetCredentials(TraceWriter log)
        {
            var clientId = ConfigurationManager.AppSettings["ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            var tenantId = ConfigurationManager.AppSettings["TenantId"];

            var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

            log.Info("Attempting to acquire token");
            var result = context.AcquireTokenAsync(
                "https://management.core.windows.net/",
                new ClientCredential(clientId, clientSecret)
            ).Result;

            return new TokenCredentials(result.AccessToken);
        }
    }
}
