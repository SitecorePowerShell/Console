<#
    .SYNOPSIS
        Returns library item or path to the library where scripts for a particular integration point should be located for a specific module.

    .DESCRIPTION
        Returns library item or path to the library where scripts for a particular integration point should be located for a specific module.

    .PARAMETER Module
        Module for which the feature root library should be returned. 
        If not provided the feature root will be returned for all modules.

    .PARAMETER Feature
        Feature for which the root library should be provided. 
        If root item does not exist and -ReturnPath parameter is not specified - nothing will be returned, 
	If -ReturnPath parameter is provided the path in which the feature root should be located will be returned

        Valid features:
        - contentEditorContextMenu 
        - contentEditorGutters
        - contentEditorRibbon
        - controlPanel
        - functions
        - listViewExport
        - listViewRibbon
        - pipelineLoggedIn
        - pipelineLoggingIn
        - pipelineLogout
        - toolbox
        - startMenuReports
	- eventHandlers
    .INPUTS
        Sitecore.Data.Items.Item
    
    .OUTPUTS
        Sitecore.Data.Items.Item
        System.String

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Get-SpeModule

    .LINK
        http://blog.najmanowicz.com/2014/11/01/sitecore-powershell-extensions-3-0-modules-proposal/

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Return the library item for "Content Editor Context Menu"
        $module = Get-SpeModule -Name "Copy Renderings"
        Get-SpeModuleFeatureRoot -Feature contentEditorContextMenu -Module $module 

    .EXAMPLE
        # Return the Path to where "List View Export" scripts would be located if this feature was defined
	$module = Get-SpeModule -Name "Copy Renderings"
	Get-SpeModuleFeatureRoot -Module $module -Feature listViewExport -ReturnPath
#>
