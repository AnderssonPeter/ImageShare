#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"


$pngResolutions = @(16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 128, 256)
$oxipng = 'D:\Tools\oxipng\oxipng.exe'
$png2ico = 'D:\Tools\go-png2ico\go-png2ico.exe'

function Write-Image {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]$source,
        [Parameter(Mandatory)]
        [string]$resolution,
        [Parameter(Mandatory)]
        [string]$extension
    )
    $filename = "icon${resolution}.${extension}"
    Write-Host "Generating icons for $filename"
    magick convert -background none -size ${resolution}x${resolution} $source $filename | Out-null
    & $oxipng -o max --strip safe $filename
}

function Generate-Icon {
    foreach ($resolution in $pngResolutions) {
        Write-Image -source icon.svg -resolution $resolution -extension 'png'
    }
    $icons = $pngResolutions | ForEach-Object { "icon$_.png" }
    & $png2ico $icons icon.ico
}


Generate-Icon