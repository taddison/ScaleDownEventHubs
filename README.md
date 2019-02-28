# ScaleDownEventHubs

Scale down Azure Event Hub Namespaces automatically.

## Deployment Guidelines

- Clone the function and modify the timer to the desired frequency (e.g. daily - defaults to every six hours)
- Deploy the function to Azure
- Create a service principal
- Set the application settings with the principal details (client id, secret, and tenant)
- Add the service principal as a contributor on all Event Hub Namespaces you want to be automaticlaly scaled down

Once deployed as an Azure function and configured with an appropriate service principal, the app will query all subscriptions it has access to and scale down every namespace it has access to to 1 throughput unit.

By default it will scale to 1 TU if the current TU is > 1.  To override this behaviour create a tag on your namespace with the key `ScaleDownTUs` and a value equal to the target number of TUs (e.g. 5).  If the namespace TU >= the tag, then no scale down operation will happen.

## Scripts

These scripts use the [Az module].

### Create a service principal

```powershell
Login-AzAccount

$sp = New-AzADServicePrincipal -DisplayName "EventHubScaler"
$applicationId = $sp.ApplicationId

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sp.Secret)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)

# Save these for later!
Write-Output "ClientId: $applicationId"
Write-Output "ClientSecret: $password"
```

### Add the service principal to all Event Hub Namespaces

This script will add the specified app as a contributor on each Event Hub Namespace.  The script below is configured in 'What If' mode, so will only report what it would do - change `$WhatIf = $true` to `$WhatIf = $false` to actually make changes.

The script will only add the permission to event hubs which are configured with auto-inflate.

```powershell
Login-AzAccount

$appName = "EventHubScaler"
$appRole = "Contributor"
$WhatIf = $true # set to false to add role

$applicationId = (Get-AzADServicePrincipal -DisplayName $appName).ApplicationId
$subs = Get-AzSubscription
foreach($sub in $subs) {
    Set-AzContext $sub | Out-Null
    Write-Output "Context set to $($sub.Name)"

    $hubs = Get-AzEventHubNamespace
    foreach($hub in $hubs) {
        $hubName = $hub.Name
        $capacity = $hub.Sku.Capacity
        $autoInflate = $hub.IsAutoInflateEnabled
        $maxCapacity = $hub.MaximumThroughputUnits

        $assignments = Get-AzRoleAssignment -Scope $hub.Id -RoleDefinitionName $appRole -ServicePrincipalName $applicationId 
        if($null -eq $assignments) {
            $assignString = ""
        } else {
            $assignString = "[$appName ASSIGNED]"
        }
        
        if($autoInflate) {
            Write-Output "Namespace:$hubName :: TU:$capacity/$maxCapacity $scaleDownTUs $assignString"

            if($null -eq $assignments) {
                if($WhatIf -eq $false) {
                    Write-Output "Adding $appRole role for $appName on $hubName"
                    New-AzRoleAssignment -Scope $hub.Id -RoleDefinitionName $appRole -ApplicationId $applicationId | Out-Null
                } else {
                    Write-Output "[WHATIF] Adding $appRole role for $appName on $hubName"
                }
            }
        } else {
            Write-Output "Namespace:$hubName :: TU:$capacity $assignString"
        }
    }
}
```

## Older versions

An earlier version of this app ([See this blog post for more details](http://tjaddison.com/2017/12/10/Auto-deflating-Event-Hubs-with-a-function-app)) required you to specify subscription ids in the function..

[Az module]: https://docs.microsoft.com/en-us/powershell/azure/install-az-ps