<#
.SYNOPSIS
    Checks that no .ts or .tsx file has an export statement on its last three lines.

.DESCRIPTION
    Recursively scans a directory for TypeScript files and inspects the last
    three non-empty lines of each. Reports every file whose tail starts an
    export. Exits 0 when none are found, 1 when at least one is, 2 on error.

    Matches: `export`, `export default`, `export type`, `export {`, `export *`,
    and `export =`. Excluded paths: node_modules, dist, .tanstack, and the
    orval-generated client (src/lib/api/generated) plus the auto-generated
    route tree (src/routeTree.gen.ts).

.EXAMPLE
    .\Check-ExportAtEnd.ps1
    .\Check-ExportAtEnd.ps1 -Directory .\frontend\src
#>
[CmdletBinding()]
param(
    [string]$Directory = (Join-Path $PSScriptRoot '..\frontend\src')
)

if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
    Write-Error "Directory not found: $Directory"
    exit 2
}

$excludeSubpaths = @('node_modules', 'dist', '.tanstack', 'lib\api\generated')
$excludeFileNames = @('routeTree.gen.ts')

$exportPattern = '^\s*export(\s+default|\s+type|\s+\*|\s*\{|\s*=|\s+\w)?'

$files = Get-ChildItem -LiteralPath $Directory -Recurse -File -Include '*.ts','*.tsx' -ErrorAction SilentlyContinue

$offendingFiles = @()
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring((Resolve-Path -LiteralPath $Directory).Path.Length + 1)

    $skip = $false
    foreach ($excluded in $excludeSubpaths) {
        if ($relativePath -like "*$excluded*" -or $relativePath -like "*$($excluded.Replace('\','/'))*") {
            $skip = $true
            break
        }
    }
    if ($skip) { continue }

    if ($excludeFileNames -contains $file.Name) { continue }

    $lines = Get-Content -LiteralPath $file.FullName
    if (-not $lines) { continue }

    $totalLineCount = $lines.Count
    $startIndex = [Math]::Max(0, $totalLineCount - 3)
    $tail = $lines[$startIndex..($totalLineCount - 1)]

    foreach ($line in $tail) {
        if ($line -match $exportPattern) {
            $offendingFiles += [PSCustomObject]@{
                File = $relativePath
                Line = $startIndex + $tail.IndexOf($line) + 1
                Content = $line.TrimStart()
            }
            break
        }
    }
}

if ($offendingFiles.Count -gt 0) {
    Write-Host "Found $($offendingFiles.Count) file(s) with an export in the last three lines:"
    $offendingFiles | Format-Table -AutoSize
    exit 1
}

Write-Host "No exports found in the last three lines of any .ts/.tsx file under $Directory"
exit 0
