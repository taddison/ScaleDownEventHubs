using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Azure.Management.EventHub;
using Microsoft.Azure.Management.EventHub.Models;

namespace ScaleDownEventHubs
{
    public static class ScaleDown
    {
        [FunctionName("ScaleDown")]
        public static void Run([TimerTrigger("0 30 1 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"ScaleDown execution started");

            log.Info("Getting credentials");
            var creds = GetCredential();

            var ns = new EventhubNamespace("<subscription id>", "<resource group>", "<namespace>", 1);

            var ehClient = new EventHubManagementClient(creds)
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

            log.Info("ScaleDown execution completed");
        }

        private static TokenCredentials GetCredential()
        {
            var clientId = ConfigurationManager.AppSettings["ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            var tenantId = ConfigurationManager.AppSettings["TenantId"];

            var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

            var result = context.AcquireTokenAsync(
                "https://management.core.windows.net/",
                new ClientCredential(clientId, clientSecret)
            ).Result;

            return new TokenCredentials(result.AccessToken);
        }
    }
}
