<#
    .SYNOPSIS
        Finds items using the Sitecore Content Search API.

    .DESCRIPTION
        The Find-Item command searches for items using the Sitecore Content Search API.

    .PARAMETER Index
        Name of the Index that will be used for the search:

        Find-Item -Index sitecore_master_index -First 10

    .PARAMETER Criteria
        Simple search criteria in the following example form:
        
        @{
            Filter = "Equals";
            Field = "_templatename";
            Value = "PowerShell Script";
        }, 
        @{
            Filter = "StartsWith";
            Field = "_fullpath";
            Value = "/sitecore/system/Modules/PowerShell/Script Library/System Maintenance";
        },
        @{
            Filter = "DescendantOf";
            Value = (Get-Item "master:/system/Modules/PowerShell/Script Library/")
        }

        Where "Filter" is one of the following values:
        - Equals
        - StartsWith
        - Contains
        - EndsWith
        - DescendantOf
        - Fuzzy
        - InclusiveRange
        - ExclusiveRange
        
        Fields by which you can filter can be discovered using the following script:

        Find-Item -Index sitecore_master_index `
                  -Criteria @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/" } `
                  -First 1 | 
            select -expand "Fields"
        
    .PARAMETER Where
        Works on Sitecore 7.5 and later versions only.

        Filtering Criteria using Dynamic Linq syntax: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .PARAMETER WhereValues
        Works on Sitecore 7.5 and later versions only.

        An Array of objects for Dynamic Linq "-Where" parameter as explained in: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .PARAMETER OrderBy
        Works on Sitecore 7.5 and later versions only.

        Field by which the search results sorting should be performed. 
        Dynamic Linq ordering syntax used.
        http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .PARAMETER First
        Number of returned search results.

    .PARAMETER Skip
        Number of search results to be skipped skip before returning the results commences.

    .PARAMETER Index
        Name of the Index to be used. Within the ISE index name autocompletion is provided. Press Ctrl+Space to be offered list of available indexes after typing "-Index".
    
    .INPUTS
    
    .OUTPUTS
        Sitecore.ContentSearch.SearchTypes.SearchResultItem

    .NOTES
        Help Author: Adam Najmanowicz, Michael West

    .LINK
        Initialize-Item

    .LINK
        Get-Item

    .LINK
        Get-ChildItem

    .LINK
        https://gist.github.com/AdamNaj/273458beb3f2b179a0b6

    .LINK
        http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library

    .LINK
        https://github.com/SitecorePowerShell/Console/

    .EXAMPLE
        # Fields by which filtering can be performed using the -Criteria parameter
        Find-Item -Index sitecore_master_index `
                  -Criteria @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/" } `
                  -First 1 | 
            select -expand "Fields"

    .EXAMPLE
        # Find all children of a specific item including that item - return Sitecore items
        $root = (Get-Item "master:/system/Modules/PowerShell/Script Library/")
        Find-Item -Index sitecore_master_index `
                  -Criteria @{Filter = "DescendantOf"; Field = $root } |
            Initialize-Item

    .EXAMPLE
        # Find all Template Fields using Dynamic LINQ syntax 
        Find-Item `
            -Index sitecore_master_index `
            -Where 'TemplateName = @0 And Language=@1' `
            -WhereValues "Template Field", "en"

    .EXAMPLE
        # Find all Template Fields using the -Criteria parameter syntax 
        Find-Item `
                -Index sitecore_master_index `
                -Criteria @{Filter = "Equals"; Field = "_templatename"; Value = "Template Field"},
                          @{Filter = "Equals"; Field = "_language"; Value = "en"}

    .EXAMPLE
        # Find items under Home that have an empty title field.
        $parameters = @{
            Index = "sitecore_master_index"
            Criteria = @(
                @{Filter = "StartsWith"; Field = "_fullpath"; Value = "/sitecore/content/home" },
                @{Filter = "Contains"; Field = "Title"; Value = ""; Invert=$true}
            )
        }

        Find-Item @parameters
#>
