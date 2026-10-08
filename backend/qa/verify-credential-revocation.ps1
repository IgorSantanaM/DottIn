[CmdletBinding()]
param(
    [string]$DockerContainer = 'charming_kare',
    [ValidateSet('Credential', 'Join')][string]$Scenario = 'Credential',
    [switch]$WithBrowser,
    [string]$AdminUrl = 'http://localhost:5231'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$settings = Get-Content -LiteralPath (Join-Path $repositoryRoot 'backend/src/DottIn.Presentation.WebApi/appsettings.Development.json') -Raw | ConvertFrom-Json
$connectionParts = [string[]]($settings.ConnectionStrings.DottInDb -split ';')
if ($DockerContainer -notmatch '^[A-Za-z0-9_.-]+$' -or
    -not ($connectionParts | Where-Object { $_ -match '^Host=(localhost|127\.0\.0\.1)$' }) -or
    -not ($connectionParts | Where-Object { $_ -eq 'Database=dottindb' })) {
    throw 'O teste de credenciais aceita apenas o PostgreSQL de desenvolvimento local.'
}
if (Get-NetTCPConnection -State Listen -LocalPort 5102 -ErrorAction SilentlyContinue) {
    throw 'A porta isolada 5102 já está em uso.'
}
if ($WithBrowser -and $Scenario -ne 'Join') { throw 'O teste de navegador está disponível apenas para convites.' }
if ($WithBrowser) {
    if ($AdminUrl -notmatch '^http://localhost:5\d{3}$') { throw 'AdminUrl deve ser localhost na porta 5000-5999.' }
    try {
        $adminResponse = Invoke-WebRequest -Uri ($AdminUrl + '/') -TimeoutSec 5 -UseBasicParsing
        if ([int]$adminResponse.StatusCode -ne 200) { throw 'Admin local não está pronto.' }
    }
    catch { throw "Inicie o Admin web em $AdminUrl antes do teste de navegador." }
}

$runId = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$testDatabase = "dottin_credential_$runId"
$testConnection = (($connectionParts | ForEach-Object {
    if ($_ -eq 'Database=dottindb') { "Database=$testDatabase" } else { $_ }
}) -join ';')
$seedPath = Join-Path $repositoryRoot 'backend/tools/seed_demo_august_2026.sql'
$containerSeedPath = "/tmp/dottin-credential-$runId.sql"
$apiDll = Join-Path $repositoryRoot 'backend/src/DottIn.Presentation.WebApi/bin/Debug/net10.0/DottIn.Presentation.WebApi.dll'
if (-not (Test-Path -LiteralPath $apiDll)) { throw 'Compile a API Debug antes deste teste.' }
$previousConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__DottInDb')
$previousEnvironment = [Environment]::GetEnvironmentVariable('ASPNETCORE_ENVIRONMENT')
$previousUrl = [Environment]::GetEnvironmentVariable('DOTTIN_QA_API_URL')
$previousMarker = [Environment]::GetEnvironmentVariable('DOTTIN_QA_CREDENTIAL_ISOLATED')
$previousJoinMarker = [Environment]::GetEnvironmentVariable('DOTTIN_QA_JOIN_ISOLATED')
$previousAdminUrl = [Environment]::GetEnvironmentVariable('DOTTIN_QA_ADMIN_URL')
$previousQaOrigin = [Environment]::GetEnvironmentVariable('AllowedOrigins__1')
$apiUrl = if ($Scenario -eq 'Join') { 'http://localhost:5102' } else { 'http://127.0.0.1:5102' }
$databaseCreated = $false
$seedCopied = $false
$apiProcess = $null

try {
    $existing = & docker exec $DockerContainer psql -U postgres -d postgres -At -c "SELECT 1 FROM pg_database WHERE datname = '$testDatabase';"
    if ($LASTEXITCODE -ne 0 -or $existing) { throw 'Não foi possível confirmar o banco temporário novo.' }
    & docker exec $DockerContainer createdb -U postgres $testDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar o banco temporário.' }
    $databaseCreated = $true

    [Environment]::SetEnvironmentVariable('ConnectionStrings__DottInDb', $testConnection)
    Push-Location $repositoryRoot
    try {
        & dotnet ef database update --project backend/src/DottIn.Infra.Data/DottIn.Infra.Data.csproj --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Migrations falharam no banco temporário.' }
    }
    finally { Pop-Location }

    & docker cp $seedPath "${DockerContainer}:$containerSeedPath"
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar o seed sintético.' }
    $seedCopied = $true
    & docker exec $DockerContainer psql -X -v ON_ERROR_STOP=1 -U postgres -d $testDatabase -f $containerSeedPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Seed sintético falhou no banco temporário.' }

    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Development')
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_API_URL', $apiUrl)
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_CREDENTIAL_ISOLATED', [string][int]($Scenario -eq 'Credential'))
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_JOIN_ISOLATED', [string][int]($Scenario -eq 'Join'))
    if ($WithBrowser) { [Environment]::SetEnvironmentVariable('DOTTIN_QA_ADMIN_URL', $AdminUrl) }
    else { [Environment]::SetEnvironmentVariable('DOTTIN_QA_ADMIN_URL', $null) }
    if ($WithBrowser) { [Environment]::SetEnvironmentVariable('AllowedOrigins__1', $AdminUrl) }
    $apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList @($apiDll, '--urls', $apiUrl) `
        -WorkingDirectory (Join-Path $repositoryRoot 'backend/src/DottIn.Presentation.WebApi') `
        -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $env:TEMP "dottin-credential-$runId.out.log") `
        -RedirectStandardError (Join-Path $env:TEMP "dottin-credential-$runId.err.log")

    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 500
        try {
            $response = Invoke-WebRequest -Uri ($apiUrl + '/health/ready') -TimeoutSec 2 -UseBasicParsing
            if ([int]$response.StatusCode -eq 200) { $ready = $true; break }
        }
        catch { }
    }
    if (-not $ready) { throw 'API isolada não ficou pronta na porta 5102.' }

    $verificationScript = if ($Scenario -eq 'Join') { 'verify-company-join-link.cjs' } else { 'verify-credential-revocation.cjs' }
    & node (Join-Path $PSScriptRoot $verificationScript)
    if ($LASTEXITCODE -ne 0) { throw "E2E $Scenario falhou." }
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -ErrorAction Stop
        $apiProcess.WaitForExit(10000) | Out-Null
    }
    [Environment]::SetEnvironmentVariable('ConnectionStrings__DottInDb', $previousConnection)
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $previousEnvironment)
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_API_URL', $previousUrl)
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_CREDENTIAL_ISOLATED', $previousMarker)
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_JOIN_ISOLATED', $previousJoinMarker)
    [Environment]::SetEnvironmentVariable('DOTTIN_QA_ADMIN_URL', $previousAdminUrl)
    [Environment]::SetEnvironmentVariable('AllowedOrigins__1', $previousQaOrigin)
    if ($seedCopied) {
        & docker exec $DockerContainer rm -- $containerSeedPath
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao remover apenas o seed temporário do container.' }
    }
    if ($databaseCreated -and $testDatabase -match '^dottin_credential_[0-9a-f]{12}$') {
        & docker exec $DockerContainer dropdb -U postgres --if-exists --force $testDatabase
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao remover apenas o banco temporário do teste.' }
    }
}
