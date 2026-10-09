[CmdletBinding()]
param(
    [ValidateSet('Local', 'Production')][string]$Mode = 'Local',
    [string]$PublicUrl,
    [switch]$RefreshWebhookSecret,
    [switch]$DisableMessaging
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$envPath = Join-Path $repoRoot '.env'
$values = [ordered]@{}
$originalValues = @{}
$existingLines = @()
if (Test-Path -LiteralPath $envPath) {
    $existingLines = @(Get-Content -LiteralPath $envPath)
    foreach ($line in $existingLines) {
        if ($line -match '^\s*([A-Z][A-Z0-9_]*)\s*=\s*(.*)$') {
            $name = $Matches[1]
            if ($originalValues.ContainsKey($name)) { throw "Duplicate .env setting $name; resolve it before initialization." }
            $value = $Matches[2].Trim()
            if ($value.StartsWith("'") -and $value.EndsWith("'")) {
                $value = $value.Substring(1, $value.Length - 2).Replace("\'", "'")
            } elseif ($value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            } else {
                $value = ($value -split '\s+#', 2)[0].Trim()
            }
            $values[$name] = $value
            $originalValues[$name] = $value
        }
    }
}

function Set-MissingValue([string]$Name, [string]$Value) {
    if ([string]::IsNullOrWhiteSpace([string]$values[$Name])) {
        $fromEnvironment = [Environment]::GetEnvironmentVariable($Name)
        if (![string]::IsNullOrWhiteSpace($fromEnvironment)) { $values[$Name] = $fromEnvironment }
        elseif (![string]::IsNullOrWhiteSpace($Value)) { $values[$Name] = $Value }
    }
}

function New-RandomSecret([switch]$Base64) {
    $bytes = New-Object byte[] 32
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    if ($Base64) { return [Convert]::ToBase64String($bytes) }
    return [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
}

Set-MissingValue APP_PUBLIC_URL $PublicUrl
if ($values['APP_PUBLIC_URL'] -eq 'https://app.example.com') { $values['APP_PUBLIC_URL'] = $PublicUrl }
if ($Mode -eq 'Local') { Set-MissingValue APP_PUBLIC_URL 'http://localhost:32850' }
$origin = $null
if (![Uri]::TryCreate([string]$values['APP_PUBLIC_URL'], [UriKind]::Absolute, [ref]$origin) -or
    $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.Fragment -or $origin.UserInfo) {
    throw 'Specify a valid origin with -PublicUrl, without a path, query or credentials.'
}
if ($Mode -eq 'Local' -and (!$origin.IsLoopback -or $origin.Port -ne 32850 -or $origin.Scheme -notin @('http', 'https'))) {
    throw 'Local mode requires a localhost origin on port 32850. Existing public configuration was not changed.'
}
if ($Mode -eq 'Production' -and ($origin.Scheme -ne 'https' -or $origin.IsLoopback)) {
    throw 'Production mode requires a public HTTPS origin. Existing local configuration was not changed.'
}
$values['APP_PUBLIC_URL'] = $origin.GetLeftPart([UriPartial]::Authority)
$values['COMPOSE_PATH_SEPARATOR'] = ';'
$values['COMPOSE_FILE'] = 'compose.yaml'
if ($Mode -eq 'Local') { $values['COMPOSE_FILE'] = 'compose.yaml;compose.local.yaml' }

foreach ($name in @('POSTGRES_PASSWORD', 'RABBITMQ_PASSWORD', 'JWT_SECRET')) {
    Set-MissingValue $name (New-RandomSecret)
}
foreach ($name in @('MT_LICENSE', 'MEDIATR_LICENSE_KEY', 'AZURE_BLOB_CONNECTION_STRING',
        'STRIPE_SECRET_KEY', 'STRIPE_PUBLISHABLE_KEY', 'STRIPE_WEBHOOK_SECRET')) {
    Set-MissingValue $name ''
}
Set-MissingValue API_ALLOWED_HOSTS '*'
Set-MissingValue AZURE_BLOB_CONTAINER_NAME 'employee-images'
Set-MissingValue APPLY_MIGRATIONS_ON_STARTUP 'true'
Set-MissingValue TOOLS_BIND_ADDRESS '127.0.0.1'
Set-MissingValue MASSTRANSIT_DISABLED $(if ($Mode -eq 'Local' -and !$values['MT_LICENSE']) { 'true' } else { 'false' })
if ($DisableMessaging) { $values['MASSTRANSIT_DISABLED'] = 'true' }
if ($Mode -eq 'Local') {
    Set-MissingValue AZURITE_ACCOUNT_KEY (New-RandomSecret -Base64)
    Set-MissingValue AZURE_BLOB_CONNECTION_STRING ("DefaultEndpointsProtocol=http;AccountName=dottinlocal;AccountKey=" +
        $values['AZURITE_ACCOUNT_KEY'] + ';BlobEndpoint=http://azurite:10000/dottinlocal;')
}

# Read local credentials in memory only. Never log keys or add them to tracked files.
$projectPath = Join-Path $repoRoot 'backend/src/DottIn.Presentation.WebApi/DottIn.Presentation.WebApi.csproj'
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$secretsId = [string]$project.Project.PropertyGroup.UserSecretsId
$userSecrets = $null
if ($env:APPDATA) {
    $secretPath = Join-Path $env:APPDATA "Microsoft/UserSecrets/$secretsId/secrets.json"
} else {
    $secretPath = Join-Path ([Environment]::GetFolderPath('UserProfile')) ".microsoft/usersecrets/$secretsId/secrets.json"
}
if (Test-Path -LiteralPath $secretPath) { $userSecrets = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json }
function Read-UserSecret([string]$Section, [string]$Key) {
    if ($null -eq $userSecrets) { return '' }
    $flat = $userSecrets.PSObject.Properties["${Section}:$Key"]
    if ($flat) { return [string]$flat.Value }
    $nested = $userSecrets.PSObject.Properties[$Section]
    if ($nested) { return [string]$nested.Value.$Key }
    return ''
}
if ($Mode -eq 'Production') {
    Set-MissingValue AZURE_BLOB_CONNECTION_STRING (Read-UserSecret AzureBlob ConnectionString)
}
$candidateSecret = Read-UserSecret Stripe SecretKey
$candidatePublic = Read-UserSecret Stripe PublishableKey
if (!$candidateSecret -or !$candidatePublic) {
    $candidateSecret = ''
    $candidatePublic = ''
    $stripeConfig = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.config/stripe/config.toml'
    if (Test-Path -LiteralPath $stripeConfig) {
        $section = ''
        foreach ($line in Get-Content -LiteralPath $stripeConfig) {
            if ($line -match '^\s*\[(.+)\]\s*$') { $section = $Matches[1] }
            if ($section -eq 'default' -and $line -match '^\s*(test_mode_api_key|test_mode_pub_key)\s*=\s*["''](.*)["'']\s*$') {
                if ($Matches[1] -eq 'test_mode_api_key') { $candidateSecret = $Matches[2] }
                else { $candidatePublic = $Matches[2] }
            }
        }
    }
}
# Import complete test-mode pairs only, and never pair a different account's keys.
if ($Mode -eq 'Local' -and $candidateSecret -match '^(sk|rk)_test_' -and $candidatePublic -match '^pk_test_' -and
    (!$values['STRIPE_SECRET_KEY'] -or $values['STRIPE_SECRET_KEY'] -eq $candidateSecret) -and
    (!$values['STRIPE_PUBLISHABLE_KEY'] -or $values['STRIPE_PUBLISHABLE_KEY'] -eq $candidatePublic)) {
    Set-MissingValue STRIPE_SECRET_KEY $candidateSecret
    Set-MissingValue STRIPE_PUBLISHABLE_KEY $candidatePublic
}
if ($Mode -eq 'Local' -and $values['STRIPE_SECRET_KEY'] -and $values['STRIPE_SECRET_KEY'] -notmatch '^(sk|rk)_test_') {
    throw 'Local mode refuses a live or unrecognized Stripe secret key. No environment file was written.'
}
if ($Mode -eq 'Local' -and $values['STRIPE_PUBLISHABLE_KEY'] -and $values['STRIPE_PUBLISHABLE_KEY'] -notmatch '^pk_test_') {
    throw 'Local mode refuses a live or unrecognized Stripe publishable key. No environment file was written.'
}
if ($Mode -eq 'Local' -and $values['STRIPE_SECRET_KEY'] -and
    (!$values['STRIPE_WEBHOOK_SECRET'] -or $RefreshWebhookSecret)) {
    if (Get-Command stripe -ErrorAction SilentlyContinue) {
        $previousApiKey = [Environment]::GetEnvironmentVariable('STRIPE_API_KEY')
        try {
            # Environment avoids including the secret in command-line arguments/process listings.
            $env:STRIPE_API_KEY = $values['STRIPE_SECRET_KEY']
            $previousErrorPreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            $cliOutput = (& stripe listen --print-secret --skip-update 2>&1 | Out-String)
            $cliSucceeded = $LASTEXITCODE -eq 0
            $ErrorActionPreference = $previousErrorPreference
            if ($cliSucceeded -and $cliOutput -match '(whsec_[A-Za-z0-9]+)') {
                $values['STRIPE_WEBHOOK_SECRET'] = $Matches[1]
                Write-Host 'Stripe CLI: test listener signing secret obtained.'
            } else { Write-Warning 'Stripe CLI could not obtain a signing secret. Run stripe login and rerun this script.' }
        } finally {
            [Environment]::SetEnvironmentVariable('STRIPE_API_KEY', $previousApiKey)
            $ErrorActionPreference = 'Stop'
        }
    } else { Write-Warning 'Stripe CLI was not found. Install it, run stripe login, and rerun this script.' }
}

$required = @('APP_PUBLIC_URL', 'POSTGRES_PASSWORD', 'RABBITMQ_PASSWORD', 'JWT_SECRET',
    'AZURE_BLOB_CONNECTION_STRING', 'STRIPE_SECRET_KEY', 'STRIPE_PUBLISHABLE_KEY', 'STRIPE_WEBHOOK_SECRET')
$missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([string]$values[$_]) })
# Preserve unchanged lines (including interpolation/quoting), comments and unrelated settings.
$lines = New-Object 'Collections.Generic.List[string]'
$written = @{}
foreach ($line in $existingLines) {
    if ($line -match '^\s*([A-Z][A-Z0-9_]*)\s*=') {
        $name = $Matches[1]
        if ($written.ContainsKey($name)) { throw "Duplicate .env setting $name; resolve it before initialization." }
        $written[$name] = $true
        $value = [string]$values[$name]
        if ($value -match '[\r\n]') { throw "Multiline value for $name is not supported; no file was written." }
        if ($value -ceq [string]$originalValues[$name]) { $lines.Add($line) }
        else { $lines.Add("$name='" + $value.Replace("'", "\'") + "'") }
    } else { $lines.Add($line) }
}
if ($existingLines.Count -eq 0) { $lines.Add('# Private Compose configuration. Generated locally; do not commit or share.'); $lines.Add('') }
foreach ($name in $values.Keys) {
    if (!$written.ContainsKey($name)) {
        $value = [string]$values[$name]
        if ($value -match '[\r\n]') { throw "Multiline value for $name is not supported; no file was written." }
        $lines.Add("$name='" + $value.Replace("'", "\'") + "'")
    }
}
[IO.File]::WriteAllLines($envPath, $lines, (New-Object Text.UTF8Encoding $false))
Write-Host "Private configuration saved to $envPath ($Mode). Existing passwords were preserved."
if ($values['MASSTRANSIT_DISABLED'] -eq 'true') {
    Write-Warning 'Core-only mode: background messaging/image uploads are disabled. Set MT_LICENSE and MASSTRANSIT_DISABLED=false to enable them.'
} elseif (!$values['MT_LICENSE']) { Write-Warning 'MT_LICENSE is missing: MassTransit 9 messaging will prevent API startup.' }
if ($Mode -eq 'Production' -and !$values['MEDIATR_LICENSE_KEY']) { Write-Warning 'MEDIATR_LICENSE_KEY is not configured; check production licensing.' }
if ($missing.Count -gt 0) {
    throw ('Configuration is incomplete. Supply these values through environment variables or .env: ' + ($missing -join ', '))
}
Write-Host 'All required Compose interpolation settings are populated. No containers were started.'
if ($Mode -eq 'Local') {
    Write-Host 'Start: docker compose up -d --build'
    Write-Host 'Webhooks (separate terminal): stripe listen --forward-to http://localhost:32850/api/webhooks/stripe'
    Write-Host 'If your CLI profile differs from the saved app account, select the matching Stripe profile before listening.'
}
