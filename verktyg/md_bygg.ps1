# Bygger Heart of NTI: Millennium Dawn (submod till Millennium Dawn).
#
# Byter ALLA porträtt (ledare, generaler, rådgivare) för länderna i Heart of NTI 4
# mot samma personer. Källbilderna är de redan beskurna porträtten i kallor/ (från Heart of NTI 4).
#
# Användning (PowerShell):
#   .\verktyg\md_bygg.ps1                 # alla länder
#   .\verktyg\md_bygg.ps1 -Lander SWE,USA # bara några
#
# Parametrar:
#   -MD    mappen där Millennium Dawn ligger (Steam Workshop 2777392649)
#   -Spel  mappen där HoI4 ligger
#   -Ut    mappen modden skrivs till (standard: mod/)
param(
    [string[]]$Lander,
    [string]$MD = "C:\Program Files (x86)\Steam\steamapps\workshop\content\394360\2777392649",
    [string]$Spel = "C:\Program Files (x86)\Steam\steamapps\common\Hearts of Iron IV",
    [string]$Ut
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (-not $Ut) { $Ut = Join-Path $repo 'mod' }
$gammal = Join-Path $repo 'kallor'
if (-not ('Dds' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'Dds.cs') -ReferencedAssemblies System.Drawing }

# MD-tagg -> mapp i gamla modden. Norge heter NRY och Jugoslavien motsvaras av Serbien (SER) i MD.
$karta = [ordered]@{
    USA='USA'; JAP='JAP'; ITA='ITA'; FIN='FIN'; SWE='SWE'; KOR='KOR'; FRA='FRA'; SOV='SOV'; GRE='GRE'
    TUR='TUR'; PER='PER'; ENG='ENG'; DEN='DEN'; SWI='SWI'; NRY='NOR'; POR='POR'; SER='YUG'; HOL='HOL'
    BEL='BEL'; ALB='ALB'; AUS='AUS'; CZE='CZE'; POL='POL'; KUR='KUR'; RAJ='RAJ'; CHL='CHL'; VEN='VEN'
    PRU='PRU'; URG='URG'; BRA='BRA'; BOL='BOL'; ECU='ECU'; COL='COL'; ARG='NTI4ARG'
}
if ($Lander) { $valda = @($Lander | ForEach-Object { $_ -split "," } | Where-Object { $_ }) } else { $valda = @($karta.Keys) }

$delad = 'generic|placeholder|leaders/(europe|asia|arabia|africa|southamerica|south_america|latin)'

function Hitta-Fil($rel) {
    foreach ($bas in @($MD, $Spel)) { $p = Join-Path $bas $rel; if (Test-Path -LiteralPath $p) { return $p } }
    return $null
}

$totalt = 0
foreach ($tag in $valda) {
    $kalla = Join-Path $gammal $karta[$tag]
    # Källbilder: stora (156x210) och, för Argentina, färdiga små ikoner.
    $alla = Get-ChildItem -LiteralPath $kalla -Filter *.dds -Recurse
    $stora = @($alla | Where-Object { ([Dds]::Size($_.FullName))[1] -gt 150 })
    $sma = @($alla | Where-Object { ([Dds]::Size($_.FullName))[1] -le 150 })
    $storaBild = @($stora | ForEach-Object { [Dds]::Load($_.FullName) })
    # Gamla mappar kan innehålla bilder på andra länders personer: använd bara den vanligaste.
    # (Argentina har många olika katter med flit.)
    if ($tag -ne 'ARG' -and $storaBild.Count -gt 1) { $storaBild = @([Dds]::Majority([System.Drawing.Bitmap[]]$storaBild)) }
    # Liten ikon = övre/mittre delen av det stora porträttet (ansiktet), om det inte finns färdiga.
    if ($sma.Count) { $smaBild = @($sma | ForEach-Object { [Dds]::Load($_.FullName) }) }
    else { $smaBild = @($storaBild | ForEach-Object { [Dds]::Crop($_, 0, 18, 156, 160) }) }

    # Mål: alla porträtt i MD:s mapp för landet + porträtt som landets karaktärer pekar på någon annanstans.
    $mal = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $mapp = Join-Path $MD "gfx\leaders\$tag"
    if (Test-Path $mapp) {
        Get-ChildItem $mapp -Filter *.dds -Recurse | ForEach-Object { [void]$mal.Add($_.FullName.Substring($MD.Length + 1).Replace('\', '/')) }
    }
    $char = Join-Path $MD "common\characters\$tag.txt"
    if (Test-Path $char) {
        $text = Get-Content -Raw -Encoding UTF8 $char
        foreach ($m in [regex]::Matches($text, '(?:large|small)\s*=\s*"?([^\s"}]+\.dds)')) {
            $rel = $m.Groups[1].Value
            # Hoppa över bilder i ett ANNAT lands mapp (t.ex. Irans karaktärer som använder Iraks bilder).
            if ($rel -match '^gfx/leaders/([A-Z]{3})/' -and $Matches[1] -ne $tag) { continue }
            if ($rel -notmatch $delad -and (Hitta-Fil $rel)) { [void]$mal.Add($rel) }
        }
    }

    $i = 0
    foreach ($rel in $mal) {
        $src = Hitta-Fil $rel
        $sz = [Dds]::Size($src); $w = $sz[0]; $h = $sz[1]
        if ($w -le 0 -or $h -le 0 -or $w -gt 4096 -or $h -gt 4096) { Write-Warning "Hoppar över $rel (konstig storlek ${w}x$h)"; continue }
        if ($h -gt 150) { $b = $storaBild[$i % $storaBild.Count] } else { $b = $smaBild[$i % $smaBild.Count] }
        try { $ny = [Dds]::Cover($b, $w, $h) } catch { Write-Warning "Kunde inte göra $rel (${w}x$h): $_"; continue }
        [Dds]::SaveDxt5($ny, (Join-Path $Ut $rel))
        $ny.Dispose(); $i++
    }
    Write-Host ("{0,-4} {1,4} porträtt  (källa: {2}, {3} bilder)" -f $tag, $mal.Count, $karta[$tag], $storaBild.Count)
    $totalt += $mal.Count
}
Write-Host "Klart: $totalt porträtt skrivna till $Ut"
