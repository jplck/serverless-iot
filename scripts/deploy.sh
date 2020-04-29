#!/bin/bash
echo "Enter app prefix: "
read APP_PREFIX
echo "Enter Resource Group name: "
read RG_NAME
echo "Enter Resource Group location (e.g. westeurope): "
read RG_LOC

#create resource group
echo "Create resource group with name $RG_NAME in ${RG_LOC}"
az group create -n $RG_NAME -l $RG_LOC --output none
echo "done"

#create storage account for static page hosting´
STORAGE_ACC_NAME="${APP_PREFIX}dashboard"
echo "Creating storage account for static page content with name $STORAGE_ACC_NAME"
az storage account create -n $STORAGE_ACC_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2" --output none
STORAGE_ACC_CONNECTION_STR=$(az storage account show-connection-string -g $RG_NAME -n $STORAGE_ACC_NAME)
echo "done"

#enabling static website on blob storage
echo "Enabling static page mode on storage account $STORAGE_ACC_NAME"
az storage blob service-properties update --account-name $STORAGE_ACC_NAME --static-website --index-document index.html --output none
DASHBOARD_URL=$(az storage account show --name $STORAGE_ACC_NAME --query 'primaryEndpoints.web')
echo "done"

#building dashboard
echo "Building static dashboard..."
cd ../src/GenericDashboard
npm install
npm run build
echo "done"

#deploying dashboard
echo "Deploying static dashboard files into storage account..."
az storage blob upload-batch -s ./build -d '$web' --account-name $STORAGE_ACC_NAME --output none
echo "done"

#create app identity in active directory
#...

#Create signalr service
SIGNALR_NAME="${APP_PREFIX}signalr"
echo "Creating signalr service with name $SIGNALR_NAME..."
az signalr create -n $SIGNALR_NAME -g $RG_NAME --sku Standard_S1 --unit-count 1 --service-mode Serverless -l $RG_LOC --output none
SIGNALR_CONNECTION_STR=$(az signalr key list -n $SIGNALR_NAME -g $RG_NAME --query primaryConnectionString -o tsv)
echo "done"

#create cosmosdb
COSMOS_NAME="${APP_PREFIX}cosmosdb"
echo "Creating cosmosdb service with name $COSMOS_NAME..."
az cosmosdb create -n $COSMOS_NAME -g $RG_NAME --locations regionName=$RG_LOC failoverPriority=0 isZoneRedundant=False --output none
COSMOS_DB_CONNECTION_STR=$(az cosmosdb keys list -n $COSMOS_NAME -g $RG_NAME --type connection-strings --query "connectionStrings[?description=='Primary SQL Connection String'].connectionString | [0]" | tr -d '"')
echo "done"

COSMOS_DB_NAME="devicedata"
echo "Creating cosmosdb database with name $COSMOS_DB_NAME..."
az cosmosdb database create -n $COSMOS_NAME -g $RG_NAME --db-name $COSMOS_DB_NAME --output none
echo "done"

COSMOS_CONTAINER_NAME="deviceusers"
COSMOS_CONTAINER_PARTITION_KEY="/deviceId"
echo "Creating cosmosdb container with name $COSMOS_CONTAINER_NAME and partition key $COSMOS_CONTAINER_PARTITION_KEY..."
az cosmosdb sql container create -g $RG_NAME -a $COSMOS_NAME -d $COSMOS_DB_NAME -n $COSMOS_CONTAINER_NAME --partition-key-path $COSMOS_CONTAINER_PARTITION_KEY --throughput "400" --output none
echo "done"

#setting up azure functions
echo "Enter name of websocket function app: "
read FUNC_WEBSOCKET_NAME
echo "Enter name of device service function app: "
read FUNC_DEV_SERVICES_NAME

az group delete -n $RG_NAME