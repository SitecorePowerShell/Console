#
# Takes translation files and turns them into a CSV equivalent, which may be easier for editing.
#

$language = "da"
$untranslatedOnly = $true

foreach ( $file in Get-ChildItem $language -Filter "*.xml" ) {
    $rows = @()

    Write-Host Processing $file.Name

    $xml = [xml](Get-Content $file.FullName)

    foreach( $phrase in $xml.SelectNodes("//phrase") )
    {
        if (!$untranslatedOnly -or $phrase.InnerText -match "^\*\*.*\*\*$")
        {
            if ($phrase.InnerText -match "^\*\*.*\*\*$") {
                $phraseText = ($phrase.InnerText.Substring(2, $phrase.InnerText.Length - 4).Trim())
            } else {
                $phraseText = $phrase.InnerText
            }

            $row = New-Object System.Object
            $row | Add-Member -type NoteProperty -name ItemID -value $phrase.ItemId
            $row | Add-Member -type NoteProperty -name Path -value $phrase.Path
            $row | Add-Member -type NoteProperty -name FieldId -value $phrase.FieldId
            $row | Add-Member -type NoteProperty -name Key -value $phrase.Key
            $row | Add-Member -type NoteProperty -name Phrase -value $phraseText
            $row | Add-Member -type NoteProperty -name TranslatedPhrase -value ""

            $rows += $row
        }
    }

    $csvFileName = $file.FullName -replace ".xml", ".csv"
    $rows | Export-Csv $csvFileName -NoTypeInformation
}