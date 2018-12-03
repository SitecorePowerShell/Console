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

$postUrl = "https://spe.dev.local/-/script/script/?user=sitecore%5Cadmin&password=b&sessionId=2c7d727c-a6ec-4849-bfff-514eaf47026d&rawOutput=False&persistentSession=False"
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
            $result = $t.Result.Content.ReadAsStringAsync().Result | ConvertFrom-CliXml
            $result
        })
        #$task = $task.ContinueWith($continuation)
        $task = Invoke-GenericMethod -InputObject $task -MethodName ContinueWith -GenericType PSObject -ArgumentList $continuation
        $task
    }
}

$tasks = [System.Threading.Tasks.Task[]]@()
$script = { Get-Location }
foreach($i in 1..20) {
    $task = Invoke-RemoteScriptAsync -Session $session -ScriptBlock $script
    $tasks += $task
}

#while($tasks.Count -gt 0 -and ($taskCompleted = [System.Threading.Tasks.Task]::WhenAny($tasks))) {
while($tasks.Count -gt 0) {
    $currentTasks = $tasks
    $tasks = @()
    foreach($task in $currentTasks) {
        if(!$task.IsCompleted) {
            $tasks += $task
        } else {
            $task.Result
        }
    }

    [System.Threading.Tasks.Task]::Delay(50) > $null
}
#[System.Threading.Tasks.Task]::WaitAll($tasks)

$watch.Stop()
$watch.ElapsedMilliseconds / 1000