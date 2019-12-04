#
# Takes translation CSV files and updates the XML files with them.
#

$language = "da"
$includeEmptyPhrases = $false

foreach ( $file in Get-ChildItem $language -Filter "*.xml" )
{
    Write-Host Processing $file.Name

    $csvFileName = $file.FullName -replace ".xml", ".csv"

    if (!(Test-Path $csvFileName)) {
        Write-Host Could not find CSV file for $file.Name -ForegroundColor Red
        continue
    }

    $xml = [xml](Get-Content $file.FullName -Encoding UTF8)
    $csv = Import-Csv $csvFileName

    foreach ( $row in $csv )
    {
        if ($row.TranslatedPhrase -or $includeEmptyPhrases) {
            $node = $xml.SelectSingleNode("//phrase[@itemid='" + $row.ItemID + "' and @fieldid='" + $row.FieldId + "']")
        
            if ($node) {
                $phraseText = $node.FirstChild

                if ($phraseText.InnerText -ne $row.TranslatedPhrase) {
                    Write-Host $phraseText.InnerText --> $row.TranslatedPhrase -ForegroundColor Green

                    $phraseText.InnerText = $row.TranslatedPhrase
                }
            } else {
                Write-Host "Could not find matching node for row: $($row)" -ForegroundColor Yellow
            }
        }
    }

    $xml.Save($file.FullName)
}