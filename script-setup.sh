#!/bin/bash
# Run this script BEFORE executing the K6 test

echo "Setting up Azure Auto-scaling for App Service Plan..."

RESOURCE_GROUP="aiva"
APP_SERVICE_PLAN="ASP-aiva-9eea"
AUTOSCALE_NAME="ASP-aiva-9eea-autoscale"

# Get subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
RESOURCE_ID="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/serverfarms/$APP_SERVICE_PLAN"

echo "Resource ID: $RESOURCE_ID"

# 1. Check App Service Plan tier
echo "1. Checking App Service Plan tier..."
az appservice plan show --name "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" --query '{name:name,sku:sku,numberOfWorkers:numberOfWorkers}' --output table

# 2. Enable Auto-scaling
echo "2. Creating auto-scale settings..."
az monitor autoscale create \
  --resource-group "$RESOURCE_GROUP" \
  --resource "$RESOURCE_ID" \
  --name "$AUTOSCALE_NAME" \
  --min-count 1 \
  --max-count 10 \
  --count 1

# 3. Add Scale-out Rule (CPU > 70%)
echo "3. Adding scale-out rule (CPU > 70%)..."
az monitor autoscale rule create \
  --resource-group "$RESOURCE_GROUP" \
  --autoscale-name "$AUTOSCALE_NAME" \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1

# 4. Add Scale-in Rule (CPU < 25%)  
echo "4. Adding scale-in rule (CPU < 25%)..."
az monitor autoscale rule create \
  --resource-group "$RESOURCE_GROUP" \
  --autoscale-name "$AUTOSCALE_NAME" \
  --condition "Percentage CPU < 25 avg 5m" \
  --scale in 1

# 5. Verify configuration
echo "5. Verifying auto-scale configuration..."
az monitor autoscale list --resource-group "$RESOURCE_GROUP" --output table

echo "✅ Auto-scaling setup complete!"
echo "🚀 You can now run: k6 run script.js"