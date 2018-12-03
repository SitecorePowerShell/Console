Import-Module -Name SPE -Force

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. $scriptDir\Invoke-GenericMethod.ps1

#https://github.com/tahir-hassan/PSRunspacedDelegate
$customType = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Runspaces;

namespace PowerShell
{
    public class RunspacedDelegateFactory
    {
        public static Delegate NewRunspacedDelegate(Delegate _delegate, Runspace runspace)
        {
            Action setRunspace = () => Runspace.DefaultRunspace = runspace;

            return ConcatActionToDelegate(setRunspace, _delegate);
        }

        private static Expression ExpressionInvoke(Delegate _delegate, params Expression[] arguments)
        {
            var invokeMethod = _delegate.GetType().GetMethod("Invoke");

            return Expression.Call(Expression.Constant(_delegate), invokeMethod, arguments);
        }

        public static Delegate ConcatActionToDelegate(Action a, Delegate d)
        {
            var parameters =
                d.GetType().GetMethod("Invoke").GetParameters()
                .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            Expression body = Expression.Block(ExpressionInvoke(a), ExpressionInvoke(d, parameters));

            var lambda = Expression.Lambda(d.GetType(), body, parameters);

            var compiled = lambda.Compile();
            
            return compiled;
        }
    }
}
"@
Add-Type -TypeDefinition $customType
Add-Type -AssemblyName System.Net.Http

Function New-RunspacedDelegate {
    param([Parameter(Mandatory=$true)][System.Delegate]$Delegate, [Runspace]$Runspace=[Runspace]::DefaultRunspace)

    [PowerShell.RunspacedDelegateFactory]::NewRunspacedDelegate($Delegate, $Runspace);
}

$session = New-ScriptSession -Username "sitecore\admin" -Password "b" -ConnectionUri "https://spe.dev.local"
$watch = [System.Diagnostics.Stopwatch]::StartNew()

function Invoke-RemoteScriptAsync {
    param(
        [pscustomobject]$Session,
        [scriptblock]$ScriptBlock,
        [switch]$Raw
    )

    $Username = $Session.Username
    $Password = $Session.Password
    $SessionId = $Session.SessionId
    $Credential = $Session.Credential
    $UseDefaultCredentials = $Session.UseDefaultCredentials
    $ConnectionUri = $Session | ForEach-Object { $_.Connection.BaseUri }
    $PersistentSession = $Session.PersistentSession

    $serviceUrl = "/-/script/script/?"
    $serviceUrl += "user=" + $Username + "&password=" + $Password + "&sessionId=" + $SessionId + "&rawOutput=" + $Raw.IsPresent + "&persistentSession=" + $PersistentSession

    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
           
    if ($Credential) {
        $handler.Credentials = $Credential
    }

    if($UseDefaultCredentials) {
        $handler.UseDefaultCredentials = $UseDefaultCredentials
    }

    $client = New-Object -TypeName System.Net.Http.Httpclient $handler

    $content = New-Object System.Net.Http.StringContent("$($ScriptBlock.ToString())<#$($SessionId)#>$($localParams)", [System.Text.Encoding]::UTF8)
    foreach($uri in $ConnectionUri) {
        $url = $uri.AbsoluteUri.TrimEnd("/") + $serviceUrl
        $localParams = ""

        $task = $client.PostAsync($url, $content)
        $continuation = New-RunspacedDelegate ([Func[System.Threading.Tasks.Task[System.Net.Http.HttpResponseMessage], PSObject]] { 
            param($t) 
        
            #Write-Host "StatusCode is $($t.Result.StatusCode)"
            $response = $t.Result.Content.ReadAsStringAsync().Result
            $response

        })
        $task = Invoke-GenericMethod -InputObject $task -MethodName ContinueWith -GenericType PSObject -ArgumentList $continuation
        $task
    }
}

#$tasks = [System.Threading.Tasks.Task[]]@()
$tasks = New-Object System.Collections.Generic.List[System.Threading.Tasks.Task]
$script = {
    #$rootId = "{371EEE15-B6F3-423A-BB25-0B5CED860EEA}"
    $rootId = "{37D08F47-7113-4AD6-A5EB-0C0B04EF6D05}"
    $itemIds = Get-ChildItem -Path "master:" -ID $rootId | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID
    $itemIds -join "|"
}

$task = Invoke-RemoteScriptAsync -Session $session -ScriptBlock $script -Raw
$tasks.Add($task) > $null

while($tasks.Count -gt 0 -and ($taskCompleted = [System.Threading.Tasks.Task]::WhenAny($tasks.ToArray()))) {
    Write-Host "Tasks remaining $($tasks.Count)"
    $currentTasks = $tasks.ToArray()
    #$tasks = New-Object System.Collections.Generic.List[System.Threading.Tasks.Task]
    foreach($task in $currentTasks) {
        if($task.Status -ne [System.Threading.Tasks.TaskStatus]::RanToCompletion) {
            #$tasks.Add($task) > $null
        } else {
            $tasks.Remove($task) > $null
            $response = $task.Result
            Write-Host "Processing response"
            if($response) {
                try{
                $itemIds = $response.Split("|", [System.StringSplitOptions]::RemoveEmptyEntries)
                Write-Host "$($itemIds.Count) Items returned"
                foreach($itemId in $itemIds) {
                    $script = { 
                        $itemIds = Get-ChildItem -Path "master:" -ID $rootId | Where-Object { $_.HasChildren } | Select-Object -ExpandProperty ID
                        $itemIds -join "|"
                    }

                    $scriptString = "`$rootId = ""$($itemId)""`n" + $script.ToString()
                    $script = [scriptblock]::Create($scriptString)
                    $task = Invoke-RemoteScriptAsync -Session $session -ScriptBlock $script -Raw
                    $tasks.Add($task) > $null
                }
                } catch {
                    Write-Host $_
                    Write-Host "Tasks remaining $($tasks.Count)"
                }
            }
            
        }
    }
}
#[System.Threading.Tasks.Task]::WaitAll($tasks)

$watch.Stop()
$watch.ElapsedMilliseconds / 1000