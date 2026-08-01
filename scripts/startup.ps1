function StopProcessTree($process) {
    if ($process -and -not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit(5000) | Out-Null
    }
}

function ShowFailureLogs($stdOutFile, $stdErrFile) {
    Write-Host '--- Log output (warnings, errors, fatals) ---'
    $allOutput = @(Get-Content $stdOutFile -ErrorAction SilentlyContinue) + @(Get-Content $stdErrFile -ErrorAction SilentlyContinue)
    $allOutput | Where-Object { $_ -match '(warn|error|crit):' } | ForEach-Object { Write-Host $_ }
}

$tempStdOut = [System.IO.Path]::GetTempFileName()
$tempStdErr = [System.IO.Path]::GetTempFileName()

try {
    $p = Start-Process dotnet -ArgumentList 'run','--project','ImageShare/ImageShare.csproj','--launch-profile','http' -PassThru -NoNewWindow -RedirectStandardOutput $tempStdOut -RedirectStandardError $tempStdErr
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep 1
        try {
            $response = Invoke-WebRequest http://localhost:5034/api/openapi/v1.json -TimeoutSec 2 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Host 'Startup verified'
                StopProcessTree $p
                exit 0
            }
        }
        catch {}
    }
    Write-Host 'Startup failed'
    StopProcessTree $p
    ShowFailureLogs $tempStdOut $tempStdErr
    exit 1
}
finally {
    Remove-Item $tempStdOut -ErrorAction SilentlyContinue
    Remove-Item $tempStdErr -ErrorAction SilentlyContinue
}
