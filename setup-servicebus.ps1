# Azure Service Bus Setup Script - BASIC TIER VERSION (CORRECTED)
# Prerequisites: Azure CLI installed and logged in
# Service Bus namespace already exists: aiva-admin-servicebus in resource group: aiva

param(
    [string]$ResourceGroup = "aiva",
    [string]$ServiceBusNamespace = "aiva-admin-servicebus"
)

Write-Host "🚀 Setting up Azure Service Bus queues and configuration..." -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host "Service Bus Namespace: $ServiceBusNamespace (Basic Tier)" -ForegroundColor Yellow

# Step 1: Create File Processing Queue (Basic Tier Compatible)
Write-Host "`n📁 Creating file-processing-queue..." -ForegroundColor Blue
az servicebus queue create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --name "file-processing-queue" `
    --max-delivery-count 5 `
    --default-message-time-to-live "PT24H" `
    --enable-dead-lettering-on-message-expiration true `
    --max-size-in-megabytes 1024 `
    --lock-duration "PT5M"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ file-processing-queue created successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to create file-processing-queue" -ForegroundColor Red
}

# Step 2: Create Title Generation Queue (Basic Tier Compatible)
Write-Host "`n💬 Creating title-generation-queue..." -ForegroundColor Blue
az servicebus queue create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --name "title-generation-queue" `
    --max-delivery-count 3 `
    --default-message-time-to-live "PT12H" `
    --enable-dead-lettering-on-message-expiration true `
    --max-size-in-megabytes 1024 `
    --lock-duration "PT3M"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ title-generation-queue created successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to create title-generation-queue" -ForegroundColor Red
}

# Step 3: Create Access Policies for File Processing Queue
Write-Host "`n🔐 Setting up access policies for file-processing-queue..." -ForegroundColor Blue

# Send policy for API
Write-Host "Creating SendPolicy for API..." -ForegroundColor Gray
az servicebus queue authorization-rule create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "file-processing-queue" `
    --name "SendPolicy" `
    --rights Send

# Listen policy for Azure Functions
Write-Host "Creating ListenPolicy for Functions..." -ForegroundColor Gray
az servicebus queue authorization-rule create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "file-processing-queue" `
    --name "ListenPolicy" `
    --rights Listen

# Manage policy for admin operations
Write-Host "Creating ManagePolicy for admin operations..." -ForegroundColor Gray
az servicebus queue authorization-rule create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "file-processing-queue" `
    --name "ManagePolicy" `
    --rights Manage Send Listen

# Step 4: Create Access Policies for Title Generation Queue
Write-Host "`n🔐 Setting up access policies for title-generation-queue..." -ForegroundColor Blue

# Send policy for API
Write-Host "Creating SendPolicy for API..." -ForegroundColor Gray
az servicebus queue authorization-rule create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "title-generation-queue" `
    --name "SendPolicy" `
    --rights Send

# Listen policy for Azure Functions
Write-Host "Creating ListenPolicy for Functions..." -ForegroundColor Gray
az servicebus queue authorization-rule create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "title-generation-queue" `
    --name "ListenPolicy" `
    --rights Listen

# Manage policy for admin operations
Write-Host "Creating ManagePolicy for admin operations..." -ForegroundColor Gray
az servicebus queue authorization-rule create `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "title-generation-queue" `
    --name "ManagePolicy" `
    --rights Manage Send Listen

# Step 5: Get Connection Strings
Write-Host "`n📋 Retrieving connection strings..." -ForegroundColor Blue

Write-Host "`n🔑 Connection Strings:" -ForegroundColor Yellow

# Primary connection string for namespace (for admin/manage operations)
Write-Host "`nNamespace Primary Connection String:" -ForegroundColor Cyan
az servicebus namespace authorization-rule keys list `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --name "RootManageSharedAccessKey" `
    --query "primaryConnectionString" `
    --output tsv

# File processing queue listen connection string
Write-Host "`nFile Processing Queue - Listen Connection String:" -ForegroundColor Cyan
az servicebus queue authorization-rule keys list `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "file-processing-queue" `
    --name "ListenPolicy" `
    --query "primaryConnectionString" `
    --output tsv

# File processing queue send connection string  
Write-Host "`nFile Processing Queue - Send Connection String:" -ForegroundColor Cyan
az servicebus queue authorization-rule keys list `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "file-processing-queue" `
    --name "SendPolicy" `
    --query "primaryConnectionString" `
    --output tsv

# Title generation queue listen connection string
Write-Host "`nTitle Generation Queue - Listen Connection String:" -ForegroundColor Cyan
az servicebus queue authorization-rule keys list `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "title-generation-queue" `
    --name "ListenPolicy" `
    --query "primaryConnectionString" `
    --output tsv

# Title generation queue send connection string
Write-Host "`nTitle Generation Queue - Send Connection String:" -ForegroundColor Cyan
az servicebus queue authorization-rule keys list `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --queue-name "title-generation-queue" `
    --name "SendPolicy" `
    --query "primaryConnectionString" `
    --output tsv

# Step 6: Verify Setup
Write-Host "`n✅ Verifying setup..." -ForegroundColor Blue

Write-Host "`nQueues in namespace:" -ForegroundColor Yellow
az servicebus queue list `
    --resource-group $ResourceGroup `
    --namespace-name $ServiceBusNamespace `
    --query "[].{Name:name, Status:status, MessageCount:messageCount}" `
    --output table

Write-Host "`n🎉 Service Bus setup completed!" -ForegroundColor Green
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Copy connection strings to your API configuration" -ForegroundColor White
Write-Host "   2. Copy connection strings to your Azure Functions configuration" -ForegroundColor White
Write-Host "   3. Update your API to send messages to the queues" -ForegroundColor White
Write-Host "   4. Create Azure Functions to process the messages" -ForegroundColor White

Write-Host "`n⚠️  Note: Basic Tier Limitations:" -ForegroundColor Yellow
Write-Host "   • No duplicate detection" -ForegroundColor White
Write-Host "   • No topics/subscriptions" -ForegroundColor White  
Write-Host "   • No auto-forwarding" -ForegroundColor White
Write-Host "   • No scheduled delivery" -ForegroundColor White