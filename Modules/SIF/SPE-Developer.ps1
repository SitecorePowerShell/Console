#msdeploy -verb:sync -source:package=%CD%\test.scwdp.zip -dest:archiveDir=%CD%\output -setParam:X=TESTVALUE

# The Prefix that will be used on SOLR, Website and Database instances.
$Prefix = "sc930"
# The Password for the Sitecore Admin User. This will be regenerated if left on the default.
$SitecoreAdminPassword = "b"
# The root folder with the license file and WDP files.
$SCInstallRoot = "C:\Sitecore\"
# The Sitecore site instance name.
$SitecoreSiteName = "$prefix.sc"
# A SQL user with sysadmin privileges.
$SqlAdminUser = "sa"
# The password for $SQLAdminUser.
$SqlAdminPassword = "12345"
# The path to the Sitecore Package to Deploy.
$Package = (Get-ChildItem "$SCInstallRoot\Sitecore.PowerShell.Extensions-6.*.scwdp.zip").FullName
$ModuleDatabase = "mastercore"

# Install XP0 via combined partials file.
$singleDeveloperParams = @{
    Path = "$SCInstallRoot\install-module.json"
    Package = $Package
    SqlServer = $SqlServer
    SqlAdminUser = $SqlAdminUser
    SqlAdminPassword = $SqlAdminPassword
    DatabasePrefix = $Prefix
    Sitename = $SitecoreSiteName
    ModuleDatabase = $ModuleDatabase
}

Push-Location $SCInstallRoot

Install-SitecoreConfiguration @singleDeveloperParams *>&1 | Tee-Object XP0-SingleDeveloper.log