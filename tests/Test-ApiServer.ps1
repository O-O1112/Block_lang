param(
    [string]$EngineDirectory = (Join-Path $PSScriptRoot '..\bin')
)

$ErrorActionPreference = 'Stop'
$EngineDirectory = [IO.Path]::GetFullPath($EngineDirectory)
$engine = Join-Path $EngineDirectory 'block.exe'
if (-not (Test-Path -LiteralPath $engine)) { throw "Missing engine: $engine" }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('block-api-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
$stdout = Join-Path $tempRoot 'server.out.log'
$stderr = Join-Path $tempRoot 'server.err.log'
$server = $null

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Get-HttpFailureStatus([scriptblock]$Request) {
    try {
        & $Request | Out-Null
        return 0
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode
        }
        throw
    }
}

try {
    $portProbe = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 0)
    $portProbe.Start()
    $port = ([Net.IPEndPoint]$portProbe.LocalEndpoint).Port
    $portProbe.Stop()

    $server = Start-Process -FilePath $engine -ArgumentList @('serve', [string]$port) -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    $token = $null
    for ($attempt = 0; $attempt -lt 100; $attempt++) {
        if ($server.HasExited) {
            $logs = ((Get-Content -LiteralPath $stdout -Raw -ErrorAction SilentlyContinue) + (Get-Content -LiteralPath $stderr -Raw -ErrorAction SilentlyContinue))
            throw "API server exited during startup with code $($server.ExitCode): $logs"
        }
        $content = Get-Content -LiteralPath $stdout -Raw -ErrorAction SilentlyContinue
        $tokenMatch = [regex]::Match([string]$content, 'Security Token:\s*([a-fA-F0-9]+)')
        if ($tokenMatch.Success) { $token = $tokenMatch.Groups[1].Value; break }
        Start-Sleep -Milliseconds 100
    }
    Assert-True (-not [string]::IsNullOrWhiteSpace($token)) 'API server did not publish its session token.'

    $base = "http://127.0.0.1:$port"
    $unauthorized = Get-HttpFailureStatus { Invoke-WebRequest -UseBasicParsing -Method Get -Uri "$base/api/status" }
    Assert-True ($unauthorized -eq 403) "Missing API token returned HTTP $unauthorized instead of 403."

    $headers = @{ 'X-Api-Token' = $token }
    $status = Invoke-RestMethod -Method Get -Uri "$base/api/status" -Headers $headers
    Assert-True ($status.status -eq 'online') 'Authenticated API status did not report online.'
    Assert-True ($status.edition -eq 'standard') 'Standard server reported the wrong edition.'
    Assert-True ($status.networkGuard -in @('advisory', 'off')) 'API status omitted the network-guard trust level.'

    $wrongEngineHeaders = @{ 'X-Api-Token' = $token; 'X-Block-Engine' = 'plus' }
    $wrongEngine = Get-HttpFailureStatus {
        Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$base/api/run" -Headers $wrongEngineHeaders -ContentType 'text/plain' -Body 'print("x")'
    }
    Assert-True ($wrongEngine -eq 409) "Engine mismatch returned HTTP $wrongEngine instead of 409."

    $runHeaders = @{
        'X-Api-Token' = $token
        'X-Block-Engine' = 'standard'
        'X-Block-Timeout-Ms' = '5000'
        'X-Block-Network-Blocked' = '1'
    }
    $result = Invoke-RestMethod -Method Post -Uri "$base/api/run" -Headers $runHeaders -ContentType 'text/plain' -Body "value = 7`nprint(value)"
    Assert-True ($result.status -eq 'success') 'Authenticated API execution did not succeed.'
    Assert-True ([string]$result.output -match '7') 'API execution output is missing.'
    Assert-True ($result.edition -eq 'standard') 'API execution response omitted the engine edition.'

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $secondOutput = (& $engine serve $port 2>&1 | Out-String).Trim()
        $secondExit = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousPreference }
    Assert-True ($secondExit -ne 0) 'A listener startup failure returned exit code 0.'
    Assert-True ($secondOutput -match 'Failed to start API Server|Access Denied') 'Listener startup failure did not provide an actionable error.'

    Write-Host 'API server integration tests passed.'
}
finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
        $server.WaitForExit(5000) | Out-Null
    }
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
