$protocolHost = "http://sitecore81"

Import-Module -Name Pester -Force

#$script = "HomeAndDescendants"
#$url = "$host/-/script/v2/master/$script?user=admin&password=b&offset=0&limit=2&fields=(Name,ItemPath,Id)"
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
            $html.GetType().Name | Should Be "XmlDocument"
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
            $html.GetType().Name | Should Be "XmlDocument"
        }
    }
}
