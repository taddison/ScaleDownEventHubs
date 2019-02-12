using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Azure.Management.EventHub;
using Microsoft.Azure.Management.EventHub.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace ScaleDownEventHubs
{
    public static class ScaleDown
    {
        [FunctionName("ScaleDown")]
        public static void Run([TimerTrigger("* */1 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"ScaleDown execution started");

            log.Info("Getting credentials");
            var creds = GetCredentials(log);

            var subscriptions = new List<string>()
            {
                "fcba2349-45f5-4b30-a9bb-b075cec3622f"
            };
            
            foreach (var subscription in subscriptions)
            {
                log.Info($"Processing scaledown for {subscription}");
                ScaleDownNamespacesInSubscription(subscription, creds, log);
            }
            
            log.Info("ScaleDown execution completed");
        }

        private static void ScaleDownNamespacesInSubscription(string subscriptionId, TokenCredentials credentials, TraceWriter log)
        {
            var ehClient = new EventHubManagementClient(credentials)
            {
                SubscriptionId = subscriptionId
            };

            log.Info($"Getting namespaces for {subscriptionId}");
            var namespaces = GetNamespacesForSubscription(ehClient);

            foreach (var ns in namespaces)
            {
                try
                {
                    ScaleDownNamespace(ns, ehClient, log);
                } catch (Exception e)
                {
                    log.Error("Error", e);
                }
            }
        }

        private static IEnumerable<EventhubNamespace> GetNamespacesForSubscription(EventHubManagementClient ehClient)
        {
            var nsList = new List<EventhubNamespace>();

            var namespaces = ehClient.Namespaces.List().ToList();


            foreach(var ns in namespaces)
            {
                var resourceGroupName = Regex.Match(ns.Id, @".*\/resourceGroups\/([\w-]+)\/providers.*").Groups[1].Value;
                int targetThroughputUnits = 1;
                if (ns.Tags.ContainsKey("ScaleDownTUs"))
                {
                    int.TryParse(ns.Tags["ScaleDownTUs"], out targetThroughputUnits);
                }
                nsList.Add(new EventhubNamespace(ehClient.SubscriptionId, resourceGroupName, ns.Name, targetThroughputUnits));
            }

            return nsList;
        }

        private static void ScaleDownNamespace(EventhubNamespace ns, EventHubManagementClient ehClient, TraceWriter log)
        {
            log.Info("Querying namespace information");
            var nsInfo = ehClient.Namespaces.Get(ns.ResourceGroup, ns.Namespace);
            if (nsInfo.Sku.Capacity <= ns.TargetThroughputUnits)
            {
                log.Info($"Namespace {ns.Namespace} in {ns.ResourceGroup} already below target capacity (Current:{nsInfo.Sku.Capacity} Target:{ns.TargetThroughputUnits})");
                return;
            }

            var nsUpdate = new EHNamespace()
            {
                Sku = new Sku(nsInfo.Sku.Name, capacity: ns.TargetThroughputUnits)
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
