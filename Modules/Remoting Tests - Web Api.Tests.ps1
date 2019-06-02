param(
    [Parameter()]
    [string]$protocolHost = "https://spe.dev.local"
)

if(!$protocolHost){
    $protocolHost = "https://spe.dev.local"
}

Import-Module -Name SPE

Describe "Web API Responses" {
    BeforeAll {
        $session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $protocolHost
        
        Invoke-RemoteScript -Session $session -ScriptBlock {
            # Getting Started
            $module = Get-Item "master:" -ID "{ED2CF34E-1A59-444D-806E-51DB1E560093}"
            $module.Enabled = "1"
        }
	    Stop-ScriptSession -Session $session
    }
    AfterAll { 
        $session = New-ScriptSession -Username "admin" -Password "b" -ConnectionUri $protocolHost
        
        Invoke-RemoteScript -Session $session -ScriptBlock {
            # Getting Started
            $module = Get-Item "master:" -ID "{ED2CF34E-1A59-444D-806E-51DB1E560093}"
            $module.Enabled = ""
        }
	    Stop-ScriptSession -Session $session
    }
    Context "POST Methods" {
        It "ChildrenAsJson script should return 6 objects as children to root item" {
            $postParams = @{user="admin"; password="b"; depth=1}
            $items = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson" -method Post -Body $postParams
            $items.Count | Should Be 6
        }
        It "ChildrenAsHtml Script should return XML Document Object" {
            $postParams = @{user="admin"; password="b"}
            $html = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml" -method Post -Body $postParams
            $html | Should BeOfType System.Xml.XmlDocument
        }
        It "HomeAndDescendants Script should return JSON object with Success" {
            $postParams = @{user="admin"; password="b"}
            $result = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/HomeAndDescendants?offset=0&limit=2&fields=(Name,ItemPath,Id)" -method Post -Body $postParams
            $result | Should BeOfType System.Management.Automation.PSCustomObject
            $result.Status | Should Be Success
            $result.Results.Count | Should Be 2
            $result.Results[0].Name | Should Be Home
        }
    }
    Context "GET Methods" {
        It "ChildrenAsJson Script - should return 6 objects as children to root item" {
            $items = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson?user=admin&password=b" 
            $items.Count | Should Be 6
        }
        It "ChildrenAsHtml script should return XML Document Object" {
            $html = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=admin&password=b"
            $html | Should BeOfType System.Xml.XmlDocument
        }
    }
    Context "Web API invalid calls" {
        It "Non existing script should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NonExistingScript?user=admin&password=b" }
            $execution | Should Throw "(404) Not Found."
        }
        It "Wrong password should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=admin&password=invalid" }
            $execution | Should Throw "(401) Unauthorized"
        }
        It "Non existing user should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=non_existing&password=invalid" }
            $execution | Should Throw "(401) Unauthorized"
        }
        It "Not found script should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NotFound" }
            $execution | Should Throw "(404) Not Found"
        }
    }
}
