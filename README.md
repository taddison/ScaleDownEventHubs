# ScaleDownEventHubs
Scale down Azure Event Hub Namespaces automatically.

Once deployed as an Azure function and configured with an appropriate service principal, the app will query all subscriptions it has access to and scale down every namespace it has access to to 1 throughput unit.

By default it will scale to 1 TU if the current TU is > 1.  To override this behaviour create a tag on your namespace with the key `ScaleDownTUs` and a value equal to the target number of TUs (e.g. 5).  If the namespace TU >= the tag, then no scale down operation will happen.

## Older versions

An earlier version of this app ([See this blog post for more details](http://tjaddison.com/2017/12/10/Auto-deflating-Event-Hubs-with-a-function-app)) required you to specify subscription ids in the function..