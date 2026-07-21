param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [string]$ExpectedEnvironment
)

$ErrorActionPreference = "Stop"

function Invoke-SmokeGet([string]$Path) {
    $uri = "$($BaseUrl.TrimEnd('/'))$Path"
    try {
        $response = Invoke-WebRequest -Uri $uri -Method Get -TimeoutSec 10 -MaximumRedirection 0
    }
    catch {
        throw "Smoke test falhou em ${Path}: $($_.Exception.Message)"
    }

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Smoke test falhou em $Path com status $($response.StatusCode)."
    }

    Write-Host "$Path OK ($($response.StatusCode))"
}

Invoke-SmokeGet "/health"
Invoke-SmokeGet "/ready"

if ($ExpectedEnvironment) {
    Write-Host "ExpectedEnvironment informado: $ExpectedEnvironment"
}
