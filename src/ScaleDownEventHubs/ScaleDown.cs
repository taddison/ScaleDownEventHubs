using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ScaleDownEventHubs
{
    public static class ScaleDown
    {
        [FunctionName("ScaleDown")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
