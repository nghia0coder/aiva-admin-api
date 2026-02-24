param(
    [string]$ResourceGroup = "aiva",
    [string]$AppName = "aiva-admin-api",
    [string]$ProjectPath = "src\Aiva.Admin.Api.Web",
    [switch]$Preview = $false
)

$ErrorActionPreference = "Stop"

function Get-UserSecrets {
    param([string]$ProjectPath)
    
    # Get UserSecretsId from project file
    $projectFile = Get-Content "$ProjectPath\*.csproj" -Raw
    $userSecretsId = [regex]::Match($projectFile, '<UserSecretsId>(.*?)</UserSecretsId>').Groups[1].Value
    
    if ([string]::IsNullOrEmpty($userSecretsId)) {
        Write-Host "❌ No UserSecretsId found in project file" -ForegroundColor Red
        return @{}
    }
    
    Write-Host "📝 UserSecretsId: $userSecretsId" -ForegroundColor Yellow
    
    # Get secrets path
    $secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$userSecretsId\secrets.json"
    
    if (-not (Test-Path $secretsPath)) {
        Write-Host "❌ No secrets found at: $secretsPath" -ForegroundColor Red
        return @{}
    }
    
    # Read and parse secrets
    $secretsJson = Get-Content $secretsPath -Raw | ConvertFrom-Json
    $secrets = @{}
    
    # Convert PSCustomObject to Hashtable
    $secretsJson.PSObject.Properties | ForEach-Object {
        $secrets[$_.Name] = $_.Value
    }
    
    Write-Host "✅ Found $($secrets.Count) secrets" -ForegroundColor Green
    return $secrets
}

function Convert-SecretKeyToAzureFormat {
    param([string]$Key)
    return $Key.Replace(":", "__").Replace(".", "_")
}

Write-Host "🔄 Migrating User Secrets to Azure App Service Environment Variables" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host "App Name: $AppName" -ForegroundColor Yellow
Write-Host "Preview Mode: $Preview" -ForegroundColor Yellow

# Get user secrets
$secrets = Get-UserSecrets -ProjectPath $ProjectPath

if ($secrets.Count -eq 0) {
    Write-Host "❌ No secrets to migrate" -ForegroundColor Red
    exit 1
}

# Convert secrets to Azure format
$azureSettings = @()
Write-Host "`n📋 Secrets to migrate:" -ForegroundColor Cyan

foreach ($secret in $secrets.GetEnumerator()) {
    $azureKey = Convert-SecretKeyToAzureFormat -Key $secret.Key
    $maskedValue = if ($secret.Value.Length -gt 10) { 
        $secret.Value.Substring(0, 4) + "****" + $secret.Value.Substring($secret.Value.Length - 4)
    } else { 
        "****" 
    }
    
    Write-Host "  $($secret.Key) -> $azureKey = $maskedValue" -ForegroundColor White
    $azureSettings += "$azureKey=$($secret.Value)"
}

if ($Preview) {
    $lineEnd = '\'
    Write-Host "`n🔍 Preview mode - showing Azure CLI command:" -ForegroundColor Magenta
    Write-Host "az webapp config appsettings set $lineEnd" -ForegroundColor Gray
    Write-Host "  --resource-group `"$ResourceGroup`" $lineEnd" -ForegroundColor Gray
    Write-Host "  --name `"$AppName`" $lineEnd" -ForegroundColor Gray
    Write-Host "  --settings $lineEnd" -ForegroundColor Gray
    
    foreach ($setting in $azureSettings) {
        Write-Host "    `"$setting`" $lineEnd" -ForegroundColor Gray
    }
    
    Write-Host "`nRun without -Preview to execute" -ForegroundColor Yellow
} else {
    Write-Host "`n☁️ Uploading settings to Azure..." -ForegroundColor Cyan
    
    # Execute Azure CLI command
    $azCommand = @(
        "webapp", "config", "appsettings", "set",
        "--resource-group", $ResourceGroup,
        "--name", $AppName,
        "--settings"
    ) + $azureSettings
    
    & az @azCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Successfully migrated $($secrets.Count) secrets to Azure!" -ForegroundColor Green
        Write-Host "🌐 Check at: https://portal.azure.com -> App Services -> $AppName -> Environment variables" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to upload settings to Azure" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n📊 Migration Summary:" -ForegroundColor Cyan
Write-Host "  Total secrets: $($secrets.Count)" -ForegroundColor White
Write-Host "  Resource Group: $ResourceGroup" -ForegroundColor White
Write-Host "  App Service: $AppName" -ForegroundColor White