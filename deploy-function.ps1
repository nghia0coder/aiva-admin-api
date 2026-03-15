#Requires -Version 7.0

# Azure Function App Deployment Script
# Assumes Function App is already created in Azure

param(
    [Parameter(Mandatory = $true)]
    [string]$FunctionAppName,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$ServiceBusConnectionString,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild
)

# Set error handling
$ErrorActionPreference = "Stop"

# Colors for output
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Blue }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }

try {
    Write-Info "Starting Azure Function deployment for: $FunctionAppName"
    
    # Get script location and set paths
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectDir = Join-Path $scriptDir "src\Aiva.Admin.Function"
    $outputDir = Join-Path $projectDir "bin\Release\net10.0"
    $publishDir = Join-Path $outputDir "publish"
    $zipPath = Join-Path $scriptDir "function-deployment.zip"
    
    Write-Info "Project directory: $projectDir"
    
    # Verify project exists
    if (!(Test-Path $projectDir)) {
        throw "Project directory not found: $projectDir"
    }
    
    # Check if Azure CLI is installed
    if (!(Get-Command "az" -ErrorAction SilentlyContinue)) {
        throw "Azure CLI is not installed. Please install it from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    }
    
    # Login check
    Write-Info "Checking Azure CLI login status..."
    $loginCheck = az account show --query "user.name" -o tsv 2>$null
    if (!$loginCheck) {
        Write-Info "Not logged in to Azure. Please login..."
        az login
        if ($LASTEXITCODE -ne 0) { throw "Azure login failed" }
    }
    Write-Success "Logged in as: $loginCheck"
    
    # Set subscription if provided
    if ($SubscriptionId) {
        Write-Info "Setting subscription: $SubscriptionId"
        az account set --subscription $SubscriptionId
        if ($LASTEXITCODE -ne 0) { throw "Failed to set subscription" }
    }
    
    # Verify Function App exists
    Write-Info "Verifying Function App exists..."
    $functionApp = az functionapp show --name $FunctionAppName --resource-group $ResourceGroupName --query "name" -o tsv 2>$null
    if (!$functionApp) {
        throw "Function App '$FunctionAppName' not found in resource group '$ResourceGroupName'"
    }
    Write-Success "Function App found: $functionApp"
    
    # Clean previous build artifacts
    if (Test-Path $outputDir) {
        Write-Info "Cleaning previous build artifacts..."
        Remove-Item $outputDir -Recurse -Force
    }
    
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    if (!$SkipBuild) {
        # Restore dependencies
        Write-Info "Restoring NuGet packages..."
        Set-Location $projectDir
        dotnet restore
        if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed" }
        Write-Success "NuGet packages restored"
        
        # Build the project
        Write-Info "Building the project..."
        dotnet build --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        Write-Success "Project built successfully"
        
        # Publish the project
        Write-Info "Publishing the project..."
        dotnet publish --configuration Release --no-build --output $publishDir
        if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
        Write-Success "Project published to: $publishDir"
    } else {
        Write-Warning "Skipping build - using existing artifacts"
        if (!(Test-Path $publishDir)) {
            throw "Published artifacts not found at: $publishDir. Remove -SkipBuild flag to build."
        }
    }
    
    # Create deployment package
    Write-Info "Creating deployment package..."
    Set-Location $publishDir
    
    # Use PowerShell Compress-Archive for cross-platform compatibility
    Compress-Archive -Path ".\*" -DestinationPath $zipPath -Force
    Write-Success "Deployment package created: $zipPath"
    
    # Deploy to Azure
    Write-Info "Deploying to Azure Function App..."
    Set-Location $scriptDir
    az functionapp deployment source config-zip --resource-group $ResourceGroupName --name $FunctionAppName --src $zipPath
    if ($LASTEXITCODE -ne 0) { throw "Deployment failed" }
    Write-Success "Deployment completed successfully!"
    
    # Configure app settings if Service Bus connection string is provided
    if ($ServiceBusConnectionString) {
        Write-Info "Updating Service Bus connection string..."
        az functionapp config appsettings set --name $FunctionAppName --resource-group $ResourceGroupName --settings "ServiceBusConnection=$ServiceBusConnectionString"
        if ($LASTEXITCODE -ne 0) { 
            Write-Warning "Failed to update Service Bus connection string. You may need to set it manually."
        } else {
            Write-Success "Service Bus connection string updated"
        }
    }
    
    # Set other required app settings
    Write-Info "Configuring function app settings..."
    $appSettings = @(
        "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated",
        "ASPNETCORE_ENVIRONMENT=Production"
    )
    
    $settingsString = $appSettings -join " "
    az functionapp config appsettings set --name $FunctionAppName --resource-group $ResourceGroupName --settings $settingsString
    if ($LASTEXITCODE -ne 0) { 
        Write-Warning "Failed to update some app settings"
    } else {
        Write-Success "App settings configured"
    }
    
    # Get function app URL
    $functionAppUrl = az functionapp show --name $FunctionAppName --resource-group $ResourceGroupName --query "defaultHostName" -o tsv
    Write-Success "Function App URL: https://$functionAppUrl"
    
    # Clean up deployment package
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
        Write-Info "Cleanup: Removed deployment package"
    }
    
    Write-Success "🎉 Deployment completed successfully!"
    Write-Info "Your Azure Function '$FunctionAppName' is now updated and running."
    
    # Show function status
    Write-Info "Checking function status..."
    az functionapp show --name $FunctionAppName --resource-group $ResourceGroupName --query "{name:name,state:state,kind:kind,location:location}" -o table
    
} catch {
    Write-Error "Deployment failed: $_"
    Write-Info "Check the error details above and try again."
    exit 1
} finally {
    # Return to script directory
    Set-Location $scriptDir
}