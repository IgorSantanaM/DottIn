[CmdletBinding()]
param(
    [string]$DockerContainer = 'charming_kare',
    [switch]$WithBrowser,
    [string]$AdminUrl = 'http://localhost:5231'
)

& (Join-Path $PSScriptRoot 'verify-credential-revocation.ps1') -DockerContainer $DockerContainer -Scenario Join -WithBrowser:$WithBrowser -AdminUrl $AdminUrl
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
