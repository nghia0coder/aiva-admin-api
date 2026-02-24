# Deploy Aiva Admin API to Azure App Service via zip
# Usage: .\deploy-aiva.ps1
# Prereqs: dotnet SDK, Azure CLI (az login)

param(
    [string]$ResourceGroup = "aiva",
    [string]$AppName = "aiva-admin-api",
    [string]$Environment = "Production"
)

$ErrorActionPreference = "Stop"
$publishDir = ".\publish"
$zipPath = ".\aiva-admin-api.zip"
$projectPath = "src\Aiva.Admin.Api.Web\Aiva.Admin.Api.Web.csproj"

Write-Host "🚀 Deploying Aiva Admin API to Azure App Service..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host "App Name: $AppName" -ForegroundColor Yellow
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Check if project exists
if (-not (Test-Path $projectPath)) {
    Write-Host "❌ Project not found: $projectPath" -ForegroundColor Red
    Write-Host "Make sure you're in the root directory of aiva-admin-api" -ForegroundColor Yellow
    exit 1
}

# Clean previous publish
Write-Host "🧹 Cleaning previous publish..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Failed to restore dependencies" -ForegroundColor Red
    exit $LASTEXITCODE 
}

# Publish project
Write-Host "🔨 Publishing Aiva.Admin.Api.Web (Release)..." -ForegroundColor Cyan
dotnet publish $projectPath -c Release -o $publishDir --self-contained false --runtime linux-x64
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Failed to publish project" -ForegroundColor Red
    exit $LASTEXITCODE 
}

# Create deployment zip
Write-Host "📦 Creating deployment zip..." -ForegroundColor Cyan
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

# Get zip file size
$zipSize = (Get-Item $zipPath).Length / 1MB
Write-Host "📊 Zip size: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Yellow

# Deploy to Azure
Write-Host "☁️ Deploying to Azure App Service: $AppName..." -ForegroundColor Cyan
az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path $zipPath --type zip
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Failed to deploy to Azure" -ForegroundColor Red
    exit $LASTEXITCODE 
}

# Set environment variables if needed
Write-Host "⚙️ Setting environment variables..." -ForegroundColor Cyan
az webapp config appsettings set --resource-group $ResourceGroup --name $AppName --settings "ASPNETCORE_ENVIRONMENT=$Environment"

# Clean up
Write-Host "🧹 Cleaning up..." -ForegroundColor Cyan
Remove-Item $publishDir -Recurse -Force
Remove-Item $zipPath -Force

Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
Write-Host "🌐 App URL: https://$AppName.azurewebsites.net" -ForegroundColor Green
Write-Host "🔍 Health Check: https://$AppName.azurewebsites.net/health" -ForegroundColor Green

# Optional: Open browser
$openBrowser = Read-Host "Open browser? (y/n)"
if ($openBrowser -eq "y" -or $openBrowser -eq "Y") {
    Start-Process "https://$AppName.azurewebsites.net"
}