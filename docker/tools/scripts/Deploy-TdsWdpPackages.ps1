[cmdletbinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$PackagesPath = ".\packages",
    [switch]$ViewLogs
)

#Extracts WDP packages to web root
function ExtractPackages($packagesPath) {
    Write-Output "Beginning package extraction"

    Get-ChildItem -Path $packagesPath -Filter "*.wdp.zip" | ForEach-Object { Expand-Archive -Path $_.FullName -DestinationPath C:\temp\TDS -Force }
    Move-Item -Path C:\temp\TDS\Content\Website\Bin\*.WebDeployClient.dll -Destination C:\inetpub\wwwroot\bin -Force
    Move-Item -Path C:\temp\TDS\Content\Website\temp\* -Destination C:\inetpub\wwwroot\temp -Force
    Remove-Item -Path C:\temp\TDS -Recurse -Force
    
    # Ensure TDS has permissions to delete items after install
    cmd /C icacls C:\inetpub\wwwroot\temp\WebDeployItems /grant 'IIS AppPool\DefaultAppPool:(OI)(CI)M'
    
    Write-Output "Package extraction complete"
}

#Invokes the deployment and waits for completion
function InvokeDeployment($viewLogs) {
    Write-Output "Beginning package deployment"

    $baseAPIUrl = "http://localhost:80/api/TDS/WebDeploy/"

    #Create the request urls
    $statusRequest = "$($baseAPIUrl)Status"
    $invokeRequest = "$($baseAPIUrl)Invoke" 
    $removeRequest = "$($baseAPIUrl)Remove" 
    $logRequest = "$($baseAPIUrl)Messages?flush=true"

    #Get the current status
    $retryCount = 20
    $requestComplete = $false;
    while($retryCount -ge 0) 
    {
        try
        {
            $statusResponse = Invoke-RestMethod -Uri $statusRequest -TimeoutSec 60

            $requestComplete = $true
            break
        }
        catch
        {
            Write-Warning "Retrying connection to $statusRequest"
            $retryCount--
        }
    }

    if (!$requestComplete)
    {
        Write-Error "Could not contact server at $statusRequest"

        exit
    }

    #See if a deployment is taking place
    if ($statusResponse.DeploymentStatus -eq "Complete" -or $statusResponse.DeploymentStatus -eq "Idle" -or $statusResponse.DeploymentStatus -eq "Failed") {
        #Call the Invoke method to start the deployment. This may not be needed if the server is restarting, but if no Assemblies or configs change
        #it will be needed 
        $invokeResponse = Invoke-RestMethod -Uri $invokeRequest -TimeoutSec 60

        if ($invokeResponse -ne "Ok")
        {
            throw "Request to start deployment failed"
        }

        Write-Output "Starting Deployment"
    }

    #Wait a bit to allow a deployment to start
    Start-Sleep -Seconds 2

    #Get the current status to see which deploy folder is being deployed
    $statusResponse = Invoke-RestMethod -Uri $statusRequest -TimeoutSec 60
    $currentDeploymentFolder = $statusResponse.CurrentDeploymentFolder
    
    while ($true) {
        #Get the current status
        $statusResponse = Invoke-RestMethod -Uri $statusRequest -TimeoutSec 60
        Write-Verbose "Server Deploy State: $($statusResponse.DeploymentStatus)"

        #If the deployment folder has changed, complete the progress
        if ($statusResponse.CurrentDeploymentFolder -ne $currentDeploymentFolder)
        {
            Write-Progress -Completed -Activity "Deploying Web Deploy Package from $currentDeploymentFolder"
        }

        #Update the progress bar
        Write-Progress -PercentComplete  $statusResponse.ProgressPercent -Activity "Deploying Web Deploy Package from $($statusResponse.CurrentDeploymentFolder)"

        #If the user wants deployment logs, write the logs
        if ($viewLogs) {
            $logResponse = Invoke-RestMethod -Uri $logRequest -TimeoutSec 60

            Write-Output $logResponse
        }

        #Stop if all deployment folders have been deployed
        if ($statusResponse.DeploymentStatus -eq "Complete" -or $statusResponse.DeploymentStatus -eq "Idle") {
            break
        }

        if ($statusResponse.DeploymentStatus -eq "Failed") {
            Write-Error "Errors detected during deployment. Please see logs for more details"

            exit 1 #set exit code to failure if there is a problem with the deployment
        }

        Start-Sleep -Seconds 5        
    }

    Write-Output "Removing installer service from webserver"

    Invoke-RestMethod -Uri $removeRequest -TimeoutSec 600

    Write-Progress -Completed -Activity "Deploying Web Deploy Package from $($statusResponse.CurrentDeploymentFolder)"
}

if (-not ([System.IO.Path]::IsPathRooted($PackagesPath))) {
    $currentDirectory = Split-Path $MyInvocation.MyCommand.Path    
    $PackagesPath = (Get-Item -Path ($currentDirectory + "\" +  $PackagesPath)).FullName
}

ExtractPackages $PackagesPath

#Wait until server has recognized that files may have changed and begins its restart
Start-Sleep -s 5

InvokeDeployment $ViewLogs

Write-Output "Package deployment complete"