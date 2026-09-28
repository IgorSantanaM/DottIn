[CmdletBinding()]
param([string]$DockerContainer = 'charming_kare')

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$settingsPath = Join-Path $repositoryRoot 'backend/src/DottIn.Presentation.WebApi/appsettings.Development.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$connectionParts = [string[]]($settings.ConnectionStrings.DottInDb -split ';')

if ($DockerContainer -notmatch '^[A-Za-z0-9_.-]+$' -or
    -not ($connectionParts | Where-Object { $_ -match '^Host=(localhost|127\.0\.0\.1)$' }) -or
    -not ($connectionParts | Where-Object { $_ -eq 'Database=dottindb' })) {
    throw 'Este smoke test aceita apenas o banco de desenvolvimento local dottindb.'
}

$runId = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$testDatabase = "dottin_mvp_migrate_$runId"
$testConnection = (($connectionParts | ForEach-Object {
    if ($_ -eq 'Database=dottindb') { "Database=$testDatabase" } else { $_ }
}) -join ';')
$previousConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__DottInDb')
$databaseCreated = $false

try {
    $existing = & docker exec $DockerContainer psql -U postgres -d postgres -At -c "SELECT 1 FROM pg_database WHERE datname = '$testDatabase';"
    if ($LASTEXITCODE -ne 0 -or $existing) {
        throw "Não foi possível confirmar que $testDatabase é um alvo novo."
    }

    & docker exec $DockerContainer createdb -U postgres $testDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível criar o banco temporário.' }
    $databaseCreated = $true

    [Environment]::SetEnvironmentVariable('ConnectionStrings__DottInDb', $testConnection)
    Push-Location $repositoryRoot
    try {
        & dotnet ef database update --project backend/src/DottIn.Infra.Data/DottIn.Infra.Data.csproj --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Migração em banco vazio falhou.' }
        $firstCount = [int]('SELECT COUNT(*) FROM "__EFMigrationsHistory";' | docker exec -i $DockerContainer psql -U postgres -d $testDatabase -At)
        if ($LASTEXITCODE -ne 0 -or $firstCount -lt 1) { throw 'Histórico de migrações ausente.' }

        & dotnet ef database update --project backend/src/DottIn.Infra.Data/DottIn.Infra.Data.csproj --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Segunda aplicação das migrações falhou.' }
        $secondCount = [int]('SELECT COUNT(*) FROM "__EFMigrationsHistory";' | docker exec -i $DockerContainer psql -U postgres -d $testDatabase -At)
        if ($LASTEXITCODE -ne 0 -or $secondCount -ne $firstCount) {
            throw 'Migrações não foram idempotentes.'
        }

        $freePlans = [int]('SELECT COUNT(*) FROM "SubscriptionPlans" WHERE "Name" = ''Free'' AND "IsActive" AND "MaxEmployees" = 5 AND "MaxBranches" = 1 AND "MonthlyPriceBRL" = 0 AND "StripePriceId" IS NULL;' | docker exec -i $DockerContainer psql -U postgres -d $testDatabase -At)
        if ($LASTEXITCODE -ne 0 -or $freePlans -ne 1) { throw 'Banco novo deve conter exatamente um plano Free ativo.' }
        Write-Output "Banco novo: $firstCount migrações; reaplicação idempotente; planos Free ativos: $freePlans."
    }
    finally { Pop-Location }
}
finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__DottInDb', $previousConnection)
    if ($databaseCreated) {
        & docker exec $DockerContainer dropdb -U postgres --if-exists --force $testDatabase
        if ($LASTEXITCODE -ne 0) {
            throw "Falha ao remover somente o banco temporário $testDatabase."
        }
    }
}
