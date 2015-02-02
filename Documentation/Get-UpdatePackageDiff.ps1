<#
    .SYNOPSIS
        Performs a diff operation between the Source and taget path akin to Sitecore Courier. The diff is the difference that takes the content of Source folder and transforms it to Target.
        IMPORTANT! This functionality requires changes to web.config file on your sitecore server to work. Please consult the first Example.

    .DESCRIPTION
        Performs a diff operation between the Source and taget path akin to Sitecore Courier. The diff is the difference that takes the content of Source folder and transforms it to Target.
        IMPORTANT! This functionality requires changes to web.config file on your sitecore server to work. Please consult the first Example.

    .PARAMETER SourcePath
        Path containing the current serialization items that needs to be transformed into Target.

    .PARAMETER TargetPath
        Path containing the desired serialization state that the Source needs to be transformed to.
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.Update.Interfaces.ICommand

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Export-UpdatePackage

    .LINK
        Install-UpdatePackage

    .LINK
        http://sitecoresnippets.blogspot.com/2012/10/sitecore-courier-effortless-packaging.html

    .LINK
        https://github.com/adoprog/Sitecore-Courier

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Required addition to web.config file for the functionality to work:
        <configuration>
          <configSections>
            <section name="sitecorediff" type="Sitecore.Update.Configuration.ConfigReader, Sitecore.Update"/>
          </configSections>
          <sitecorediff>
            <commandfilters>
              <filter id="changedFieldsFilter" mode="on" type="Sitecore.Update.Commands.Filters.ChangedFieldsFilter, Sitecore.Update">
                <fields hint="list">
                  <field>__Created</field>
                  <field>{5DD74568-4D4B-44C1-B513-0AF5F4CDA34F}</field>
                  <field>__Revision</field>
                  <field>__Updated</field>
                  <field>__Updated by</field>
                </fields>
              </filter>
            </commandfilters>
            <dataproviders>
              <dataprovider id="filesystemmain" type="Sitecore.Update.Data.Providers.FileSystemProvider, Sitecore.Update">
                <param>$(id)</param>
              </dataprovider>
              <dataprovider id="snapshotprovider" type="Sitecore.Update.Data.Providers.SnapShotProvider, Sitecore.Update">
                <param>$(id)</param>
              </dataprovider>
            </dataproviders>
        
            <source type="Sitecore.Update.Data.DataManager, Sitecore.Update">
              <param>source</param>
            </source>
        
            <target type="Sitecore.Update.Data.DataManager, Sitecore.Update">
              <param>target</param>
            </target>
          </sitecorediff>
        </configuration>
        
    .EXAMPLE
        # Create an update package that transforms the serialized database state defined in C:\temp\SerializationSource into into set defined in C:\temp\SerializationTarget
        $diff = Get-UpdatePackageDiff -SourcePath C:\temp\SerializationSource -TargetPath C:\temp\SerializationTarget
        Export-UpdatePackage -Path C:\temp\SerializationDiff.update -CommandList $diff -Name name

#>
