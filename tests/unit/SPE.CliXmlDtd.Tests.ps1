# Unit tests for ConvertFrom-CliXml DTD hardening.
#
# Security property: ConvertFrom-CliXml must not process XML document type
# definitions. CliXml is PowerShell's serialization format and never
# legitimately contains a DOCTYPE block; any DTD in an inbound payload is
# nonsense input on this code path and is treated as attack surface.
#
# Severity note (measured on .NET Framework 4.8): XmlTextReaderImpl carries
# a hardcoded MaxCharactersFromEntities = 10,000,000, so the catastrophic
# form of billion-laughs (unbounded memory) cannot occur on this stack.
# The attack that DOES work on the pre-fix code is a CPU/allocation
# amplification: a ~700-byte nested-entity payload forces ~1 second of CPU
# and ~6 MB of string allocation per request before the internal cap fires.
# Single-entity substitution (&xxe; -> "PWNED") also resolves silently on
# the deserializer output. The right fix is at the API layer (disable DTD
# processing entirely) rather than relying on an internal framework cap.
#
# Test shape: a single-entity payload is sufficient proof that DTD
# processing is active. It runs in milliseconds, avoids thrashing the
# runner, and if the parser resolves one entity it will resolve nested
# ones too.
#
# Scope:
# 1. Runtime test of the module's PS function (modules/SPE/ConvertFrom-CliXml.ps1)
# 2. Source-regression guards on the 4 known-affected locations
#    (the C# cmdlet, the PS function, and two server-serialized YAML mirrors).
#    Source guards double as a reversion tripwire: anyone who re-introduces
#    `new XmlTextReader` or drops the DtdProcessing setting fails the build.

# ============================================================
# Payloads
# ============================================================
$maliciousClixml = @'
<?xml version="1.0"?>
<!DOCTYPE root [
  <!ENTITY xxe "PWNED">
]>
<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
  <S>&xxe;</S>
</Objs>
'@

$validClixml = @'
<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
  <S>hello</S>
</Objs>
'@

# ============================================================
# PS function: malicious DTD payload must not expand
# ============================================================
Write-Host "`n  [ConvertFrom-CliXml PS function - DTD entity rejection]" -ForegroundColor White

$psOutput = $null
$psWarnings = @()
$psException = $null
try {
    $psOutput = ConvertFrom-CliXml -InputObject $maliciousClixml -WarningVariable psWarnings -WarningAction SilentlyContinue
} catch {
    $psException = $_
}

# Pre-fix: $psOutput is "PWNED" and no warning or exception is raised.
# Post-fix: entity is not resolved; parser either throws or writes a warning.
Assert-True ("$psOutput" -ne "PWNED") "PS function must not expand DTD entities to PWNED"
Assert-True ($psWarnings.Count -gt 0 -or $null -ne $psException) "PS function must signal DTD rejection (warning or exception)"

# ============================================================
# PS function: DTD-free payload still round-trips
# ============================================================
Write-Host "`n  [ConvertFrom-CliXml PS function - DTD-free round-trip]" -ForegroundColor White

$psValid = ConvertFrom-CliXml -InputObject $validClixml
Assert-Equal "$psValid" "hello" "PS function round-trips DTD-free CliXml unchanged"

# ============================================================
# Source regression guards: each affected location must use the secure API
# ============================================================
# Instantiating the C# cmdlet (ConvertFromCliXmlCommand) at runtime would
# require a Sitecore-hosted runspace because Spe.dll references Sitecore
# assemblies. A source-level regression guard catches any future reversion
# without that runtime dependency. The two YAML mirrors are server-serialized
# copies of the same PS logic and are covered identically.
Write-Host "`n  [ConvertFrom-CliXml - source regression guards]" -ForegroundColor White

$sources = @(
    @{ Path = "$PSScriptRoot\..\..\src\Spe\Commands\Remoting\ConvertFromCliXmlCommand.cs"; Name = "ConvertFromCliXmlCommand.cs" }
    @{ Path = "$PSScriptRoot\..\..\modules\SPE\ConvertFrom-CliXml.ps1"; Name = "ConvertFrom-CliXml.ps1" }
    @{ Path = "$PSScriptRoot\..\..\serialization\modules\serialization\SPE\SPE\Core\Platform\Functions\Remoting.yml"; Name = "Remoting.yml" }
    @{ Path = "$PSScriptRoot\..\..\serialization\modules\serialization\SPE\SPE\Core\Platform\Functions\Remoting2.yml"; Name = "Remoting2.yml" }
)

foreach ($s in $sources) {
    if (-not (Test-Path $s.Path)) {
        Skip-Test "$($s.Name) regression guard" "File not found"
        continue
    }
    $body = Get-Content -Raw $s.Path
    $constructsXmlTextReader = ($body -match 'new\s+XmlTextReader\s*\(') -or ($body -match 'New-Object\s+System\.Xml\.XmlTextReader')
    Assert-True (-not $constructsXmlTextReader) "$($s.Name) does not construct XmlTextReader (DTD defaults to Parse)"
    Assert-True ($body -match 'DtdProcessing') "$($s.Name) configures DtdProcessing"
}
