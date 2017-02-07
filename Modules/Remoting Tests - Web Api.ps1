$protocolHost = "http://sitecore81"

Import-Module -Name Pester -Force

#$script = "HomeAndDescendants"
#$url = "$host/-/script/v2/master/HomeAndDescendants?user=admin&password=b&offset=0&limit=2&fields=(Name,ItemPath,Id)"
#Invoke-RestMethod -Uri $url

Describe "WebAPi POST Response" {
    Context "ChildrenAsJson Script" {
        It "Should return 6 objects as children to root item" {
	    $postParams = @{user="admin"; password="b"; depth=1}
            $items = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson" -method Post -Body $postParams
            $items.Count | Should Be 6
        }
    }
    Context "ChildrenAsHtml Script" {
        It "Should return XML Document Object" {
	    $postParams = @{user="admin"; password="b"}
            $html = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml" -method Post -Body $postParams
            $html | Should BeOfType System.Xml.XmlDocument
        }
    }
    Context "HomeAndDescendants Script" {
        It "Should return JSON object with Success" {
	    $postParams = @{user="admin"; password="b"}
            $result = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/HomeAndDescendants?offset=0&limit=2&fields=(Name,ItemPath,Id)" -method Post -Body $postParams
            $result | Should BeOfType System.Management.Automation.PSCustomObject
            $result.Status | Should Be Success
            $result.Results.Count | Should Be 2
            $result.Results[0].Name | Should Be Home
        }
    }
}

Describe "WebAPi GET Response" {
    Context "ChildrenAsJson Script" {
        It "Should return 6 objects as children to root item" {
            $items = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsJson?user=admin&password=b" 
            $items.Count | Should Be 6
        }
    }
    Context "ChildrenAsHtml Script" {
        It "Should return XML Document Object" {
            $html = Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=admin&password=b"
            $html | Should BeOfType System.Xml.XmlDocument
        }
    }
}

Describe "WebAPi invalid calls" {
    Context "Non existing Script" {
        It "Should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/NonExistingScript?user=admin&password=b" }
            $execution | Should Throw "(404) Not Found."
        }
    }
    Context "Wrong password" {
        It "Should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=admin&password=invalid" }
            $execution | Should Throw "(404) Not Found"
        }
    }
    Context "Non existing user" {
        It "Should throw exception" {
            $execution = { Invoke-RestMethod -Uri "$protocolHost/-/script/v2/master/ChildrenAsHtml?user=non_existing&password=invalid" }
            $execution | Should Throw "(401) Unauthorized"
        }
    }
}
