$ErrorActionPreference = 'Stop'

$env:ASPNETCORE_URLS = 'https://localhost:18888'
$env:ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL = 'http://localhost:18889'
$env:ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL = 'http://localhost:18890'
$env:ASPIRE_ALLOW_UNSECURED_TRANSPORT = 'true'
$env:ASPNETCORE_ENVIRONMENT = 'Development'

$logDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$outLog = Join-Path $logDir 'apphost-out.log'
$errLog = Join-Path $logDir 'apphost-err.log'

$existing = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -like '*ProjectMcp.AppHost*' }
foreach ($proc in $existing) {
    Stop-Process -Id $proc.ProcessId
}

$projectPath = Join-Path $PSScriptRoot 'src\ProjectMcp.AppHost\ProjectMcp.AppHost.csproj'
dotnet build $projectPath -c Debug
$process = Start-Process -FilePath dotnet -ArgumentList "run --project `"$projectPath`" --no-build" -WorkingDirectory $PSScriptRoot -RedirectStandardOutput $outLog -RedirectStandardError $errLog -PassThru

"AppHost started (PID $($process.Id)). Dashboard: https://localhost:18888"
"Logs: $outLog"
"Errors: $errLog"
