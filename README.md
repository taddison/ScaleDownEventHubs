# ScaleDownEventHubs

Scale down Azure Event Hub Namespaces automatically.

## Deployment Guidelines

- Clone the function and modify the timer to the desired frequency (e.g. daily)
- Deploy the function to Azure
- Create a service principal
- Set the application settings with the principal details (client id, secret, and tenant)
- Add the service principal as a contributor on all Event Hub Namespaces you want to be automaticlaly scaled down

Once deployed as an Azure function and configured with an appropriate service principal, the app will query all subscriptions it has access to and scale down every namespace it has access to to 1 throughput unit.

By default it will scale to 1 TU if the current TU is > 1.  To override this behaviour create a tag on your namespace with the key `ScaleDownTUs` and a value equal to the target number of TUs (e.g. 5).  If the namespace TU >= the tag, then no scale down operation will happen.

## Scripts

### Create a service principal

```powershell
$appDisplayName = "EventHubScaler" # This can be anything you want
$appPassword = "<a strong password for your app>" # The password is your app's ClientSecret

Login-AzureRmAccount
$sp = New-AzureRmADServicePrincipal -DisplayName $appDisplayName -Password $appPassword
$sp | Select DisplayName, ApplicationId # ApplicationId is your app's ClientId
```

### Add the service principal to all Event Hub Namespaces

```powershell
#todo
```

## Older versions

An earlier version of this app ([See this blog post for more details](http://tjaddison.com/2017/12/10/Auto-deflating-Event-Hubs-with-a-function-app)) required you to specify subscription ids in the function..