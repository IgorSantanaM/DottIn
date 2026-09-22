[CmdletBinding()]
param(
    [string]$AdminBaseUrl,
    [string]$ApiBaseUrl,
    [switch]$SkipAndroid
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Invoke-CheckedDotnet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') falhou com código $LASTEXITCODE."
    }
}

function Get-CheckedResponse {
    param([string]$BaseUrl, [string]$Path)

    $uri = [Uri]::new([Uri]::new($BaseUrl.TrimEnd('/') + '/'), $Path.TrimStart('/'))
    return Invoke-WebRequest -Uri $uri -TimeoutSec 15 -SkipHttpErrorCheck
}

Push-Location $repositoryRoot
try {
    Invoke-CheckedDotnet -Arguments @('test', 'backend/DottIn.Web.slnx', '-c', 'Debug', '--no-restore', '-v:q')
    Invoke-CheckedDotnet -Arguments @('build', 'backend/clients/DottIn.Admin/DottIn.Admin.csproj', '-c', 'Release', '--no-restore', '-v:q')
    if (-not $SkipAndroid) {
        Invoke-CheckedDotnet -Arguments @('build', 'backend/clients/DottIn.Mobile/DottIn.Mobile.csproj', '-f', 'net10.0-android', '-c', 'Debug', '--no-restore', '-v:q')
    }

    if ($AdminBaseUrl) {
        foreach ($path in @('/', '/js/download.js', '/_framework/blazor.webassembly.js')) {
            $response = Get-CheckedResponse -BaseUrl $AdminBaseUrl -Path $path
            if ([int]$response.StatusCode -ne 200) {
                throw "Admin $path respondeu $([int]$response.StatusCode), esperado 200."
            }
        }
    }

    if ($ApiBaseUrl) {
        $live = Get-CheckedResponse -BaseUrl $ApiBaseUrl -Path '/health/live'
        if ([int]$live.StatusCode -ne 200) {
            throw "API /health/live respondeu $([int]$live.StatusCode), esperado 200."
        }
        if (-not $live.Headers['Server-Timing']) {
            throw 'API /health/live não enviou Server-Timing.'
        }

        $ready = Get-CheckedResponse -BaseUrl $ApiBaseUrl -Path '/health/ready'
        if ([int]$ready.StatusCode -ne 200) {
            throw "API /health/ready respondeu $([int]$ready.StatusCode), esperado 200. Verifique o banco."
        }
    }

    Write-Output 'Verificações automatizadas do MVP concluídas.'
}
finally {
    Pop-Location
}
