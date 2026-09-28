[CmdletBinding()]
param(
    [string]$DockerContainer = 'charming_kare',
    [string]$SourceDatabase = 'dottindb',
    [switch]$VerifyUpgrade
)

$ErrorActionPreference = 'Stop'

if ($DockerContainer -notmatch '^[A-Za-z0-9_.-]+$' -or
    $SourceDatabase -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw 'Container ou banco de origem inválido.'
}

function Invoke-CheckedDocker {
    param([string[]]$Arguments)

    $sqlIndex = [Array]::IndexOf($Arguments, '-c')
    if ($Arguments.Length -ge 3 -and $Arguments[0] -eq 'exec' -and
        $Arguments[2] -eq 'psql' -and $sqlIndex -ge 0) {
        # PowerShell 5 strips quoted PostgreSQL identifiers from native -c args.
        # Send the SQL over stdin so mixed-case table names survive intact.
        $dockerArguments = @('exec', '-i') + $Arguments[1..($sqlIndex - 1)]
        $output = $Arguments[$sqlIndex + 1] | & docker @dockerArguments
    }
    else {
        $output = & docker @Arguments
    }
    if ($LASTEXITCODE -ne 0) {
        throw "docker $($Arguments -join ' ') falhou com código $LASTEXITCODE."
    }
    return $output
}

$runId = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$restoreDatabase = "dottin_mvp_restore_$runId"
$dumpPath = "/tmp/dottin-mvp-restore-$runId.dump"
$databaseCreated = $false
$dumpCreated = $false
$previousConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__DottInDb')

try {
    $existing = Invoke-CheckedDocker -Arguments @(
        'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', 'postgres', '-At',
        '-c', "SELECT 1 FROM pg_database WHERE datname = '$restoreDatabase';")
    if ($existing) {
        throw "O banco temporário $restoreDatabase já existe; nenhum dado foi modificado."
    }

    Invoke-CheckedDocker -Arguments @(
        'exec', $DockerContainer, 'pg_dump', '-U', 'postgres', '-d', $SourceDatabase,
        '-Fc', '-f', $dumpPath) | Out-Null
    $dumpCreated = $true

    Invoke-CheckedDocker -Arguments @(
        'exec', $DockerContainer, 'createdb', '-U', 'postgres', $restoreDatabase) | Out-Null
    $databaseCreated = $true

    Invoke-CheckedDocker -Arguments @(
        'exec', $DockerContainer, 'pg_restore', '-U', 'postgres', '-d', $restoreDatabase,
        '--exit-on-error', $dumpPath) | Out-Null

    foreach ($table in @('Employees', 'Branches', 'SubscriptionPlans',
            'TenantSubscriptions', 'TimeKeepings', 'StripeWebhookReceipts', '__EFMigrationsHistory')) {
        $sql = "SELECT COUNT(*) FROM `"$table`";"
        $sourceCount = [long](Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $SourceDatabase,
            '-At', '-c', $sql))
        $restoredCount = [long](Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $restoreDatabase,
            '-At', '-c', $sql))
        if ($sourceCount -ne $restoredCount) {
            throw "Restauração divergente em $table`: origem=$sourceCount, restaurado=$restoredCount."
        }
        Write-Output "$table`: $restoredCount linhas conferidas."
    }

    if ($VerifyUpgrade) {
        if ($SourceDatabase -ne 'dottindb') {
            throw 'O teste de upgrade aceita somente o banco de desenvolvimento local dottindb.'
        }
        $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
        $settingsPath = Join-Path $repositoryRoot 'backend/src/DottIn.Presentation.WebApi/appsettings.Development.json'
        $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
        $connectionParts = [string[]]($settings.ConnectionStrings.DottInDb -split ';')
        if (-not ($connectionParts | Where-Object { $_ -match '^Host=(localhost|127\.0\.0\.1)$' }) -or
            -not ($connectionParts | Where-Object { $_ -eq 'Database=dottindb' })) {
            throw 'O teste de upgrade requer apenas a conexão local de desenvolvimento.'
        }
        $testConnection = (($connectionParts | ForEach-Object {
            if ($_ -eq 'Database=dottindb') { "Database=$restoreDatabase" } else { $_ }
        }) -join ';')
        $originalFreeId = Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $restoreDatabase,
            '-At', '-c', 'SELECT "Id" FROM "SubscriptionPlans" WHERE "Name" = ''Free'';')
        $originalMigrations = [int](Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $restoreDatabase,
            '-At', '-c', 'SELECT COUNT(*) FROM "__EFMigrationsHistory";'))
        [Environment]::SetEnvironmentVariable('ConnectionStrings__DottInDb', $testConnection)
        Push-Location $repositoryRoot
        try {
            & dotnet ef database update --project backend/src/DottIn.Infra.Data/DottIn.Infra.Data.csproj --no-build
            if ($LASTEXITCODE -ne 0) { throw 'Upgrade da cópia restaurada falhou.' }
        }
        finally { Pop-Location }
        $updatedMigrations = [int](Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $restoreDatabase,
            '-At', '-c', 'SELECT COUNT(*) FROM "__EFMigrationsHistory";'))
        if ($updatedMigrations -lt $originalMigrations) { throw 'Upgrade perdeu histórico de migrações.' }
        $updatedFreeId = Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $restoreDatabase,
            '-At', '-c', 'SELECT "Id" FROM "SubscriptionPlans" WHERE "Name" = ''Free'' AND "IsActive";')
        if (-not $updatedFreeId -or ($originalFreeId -and $updatedFreeId -ne $originalFreeId)) {
            throw 'Upgrade não preservou o plano Free existente.'
        }
        foreach ($table in @('Employees', 'Branches', 'SubscriptionPlans',
                'TenantSubscriptions', 'TimeKeepings', 'StripeWebhookReceipts')) {
            $sql = "SELECT COUNT(*) FROM `"$table`";"
            $sourceCount = [long](Invoke-CheckedDocker -Arguments @(
                'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $SourceDatabase,
                '-At', '-c', $sql))
            $upgradedCount = [long](Invoke-CheckedDocker -Arguments @(
                'exec', $DockerContainer, 'psql', '-U', 'postgres', '-d', $restoreDatabase,
                '-At', '-c', $sql))
            if ($sourceCount -ne $upgradedCount) { throw "Upgrade alterou a contagem de $table." }
        }
        Write-Output "Upgrade da cópia: $originalMigrations -> $updatedMigrations migrações; dados e plano Free preservados."
    }

    Write-Output 'Backup e restauração local do PostgreSQL concluídos; banco de origem preservado.'
}
finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__DottInDb', $previousConnection)
    if ($databaseCreated) {
        Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'dropdb', '-U', 'postgres', '--if-exists',
            '--force', $restoreDatabase) | Out-Null
    }
    if ($dumpCreated) {
        Invoke-CheckedDocker -Arguments @(
            'exec', $DockerContainer, 'rm', '--', $dumpPath) | Out-Null
    }
}
