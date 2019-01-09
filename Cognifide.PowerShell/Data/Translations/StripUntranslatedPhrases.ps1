#
# Processes the language file and strips out the ** prefix + suffix from all phrases, which represent untranslated phrases. 
# This allows the file to be imported into Sitecore despite not being fully translated.
#

$language = "de-de"

foreach ( $file in Get-ChildItem $language -Filter "*.xml")
{
    $xml = [xml](Get-Content $file.FullName)

    foreach( $phraseText in $xml.SelectNodes("//phrase/*[1]") )
    {
        if ($phraseText.InnerText -match "^\*\*.*\*\*$")
        {
            Write-Host Stripping: $phraseText.InnerText

            $phraseText.InnerText = $phraseText.InnerText.Substring(2, $phraseText.InnerText.Length - 4).Trim()
        }
    }

    $xml.Save($file.FullName)
}