$p = Start-Process dotnet -ArgumentList 'run','--project','ImageShare/ImageShare.csproj','--launch-profile','http' -PassThru -NoNewWindow
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep 1
    try {
        Invoke-WebRequest http://localhost:5034 -TimeoutSec 2 -UseBasicParsing | Out-Null
        Write-Host 'Startup verified'
        $p.Kill()
        exit 0
    }
    catch {}
}
Write-Host 'Startup failed'
$p.Kill()
exit 1
