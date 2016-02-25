$url = "http://console/-/script/v2/master/HomeAndDescendants?user=admin&password=b&offset=0&limit=2&fields=(Name,ItemPath,Id)"
Invoke-RestMethod -Uri $url