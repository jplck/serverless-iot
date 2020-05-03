#!/bin/bash
echo "Enter app prefix: "
read APP_PREFIX
echo "Enter Resource Group name: "
read RG_NAME
echo "Enter Resource Group location (e.g. westeurope): "
read RG_LOC

SUBSCRIPTION_ID=$(az account show --query "id" | tr -d '"')

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
DASHBOARD_URL=$(az storage account show --name $STORAGE_ACC_NAME --query 'primaryEndpoints.web' | tr -d '"' | awk -F[/:] '{print $4}')
echo "done"

#creating API Management
APIM_NAME="${APP_PREFIX}apim"
echo "Create APIM with name $APIM_NAME"
az apim create -g $RG_NAME -l $RG_LOC --sku-name Consumption --publisher-email test@example5325235dsgsdgdsg.com --publisher-name testorg --name $APIM_NAME
APIM_GW_HOST=$(az apim list --query "[].hostnameConfigurations[].hostName | [0]" | tr -d '"')

#creating API Management
FD_NAME="${APP_PREFIX}azfd"
FD_LB_SETTINGS_NAME="${APP_PREFIX}azfdlbsettings"
FD_BACKEND_POOL_APIM_NAME="apim"
FD_BACKEND_POOL_DASHBOARD_NAME="dashboard"
FD_HOST="${FD_NAME}demo.azurefd.net"
echo "Create FrontDoors with name $FD_NAME"
az network front-door create -g $RG_NAME --backend-address $FD_HOST --name $FD_NAME --output none
echo "Create FrontDoors load balancing settings."
az network front-door load-balancing create --front-door-name $FD_NAME --name $FD_LB_SETTINGS_NAME --resource-group $RG_NAME --sample-size 4 --successful-samples-required 2 --output none
echo "Create FrontDoors probe settings."
az network front-door probe create --front-door-name $FD_NAME --interval 30 --name "probesettings" --path "/" --resource-group $RG_NAME --enabled Disabled --output none
echo "Create FrontDoors backend pools."
az network front-door backend-pool create --address $APIM_GW_HOST --front-door-name $FD_NAME --load-balancing $FD_LB_SETTINGS_NAME --name $FD_BACKEND_POOL_APIM_NAME -g $RG_NAME --probe "probesettings" --output none
az network front-door backend-pool create --address $DASHBOARD_URL --front-door-name $FD_NAME --load-balancing $FD_LB_SETTINGS_NAME --name $FD_BACKEND_POOL_DASHBOARD_NAME -g $RG_NAME --probe "probesettings" --output none
echo "Create FrontDoors routing rules."
az network front-door routing-rule create --front-door-name $FD_NAME --frontend-endpoints "DefaultFrontendEndpoint" --name "apimrule" --resource-group $RG_NAME --route-type Forward --backend-pool $FD_BACKEND_POOL_APIM_NAME --accepted-protocols Https --output none
az network front-door routing-rule create --front-door-name $FD_NAME --frontend-endpoints "DefaultFrontendEndpoint" --name "dashboardrule" --resource-group $RG_NAME --route-type Forward --backend-pool $FD_BACKEND_POOL_DASHBOARD_NAME --accepted-protocols Https --patterns "/dashboard/*" --output none

FRONT_DOOR_DASHBOARD_ENDPOINT="https://${FD_NAME}.azurefd.net/${FD_BACKEND_POOL_DASHBOARD_NAME}/"

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

#creating iot hub
IOT_HUB_NAME="${APP_PREFIX}hub352225ghtf46"
IOT_HUB_EVENTHUB_NS_NAME="${APP_PREFIX}eventhubns124"
IOT_HUB_EVENTHUB_NAME="${APP_PREFIX}eventhub124"
IOT_HUB_EVENTHUB_AUTH_RULE_NAME="owner"
IOT_HUB_ROUTE_ENDPOINT_NAME="Telemetry"
IOT_HUB_ROUTE_NAME="TelemetryRoute"
echo "Creating IoT Hub with name $IOT_HUB_NAME..."
az iot hub create -g $RG_NAME -n $IOT_HUB_NAME --sku S1 --partition-count 2 -l $RG_LOC
IOT_HUB_CONNECTION_STR=$(az iot hub show-connection-string -n iotdemonstratorhub1 -g iotdemonstrator --query "connectionString" | tr -d '"')
#create iot hub event hub
echo "Creating Event Hub Namespace with name $IOT_HUB_EVENTHUB_NS_NAME..."
az eventhubs namespace create --resource-group $RG_NAME --name $IOT_HUB_EVENTHUB_NS_NAME --location $RG_LOC --sku Standard --enable-auto-inflate --maximum-throughput-units 2 --output none
echo "Creating Event Hub with name $IOT_HUB_EVENTHUB_NAME..."
az eventhubs eventhub create --resource-group $RG_NAME --namespace-name $IOT_HUB_EVENTHUB_NS_NAME --name $IOT_HUB_EVENTHUB_NAME --message-retention 4 --partition-count 3 --output none
echo "Creating Event Hub auth rule with name $IOT_HUB_EVENTHUB_AUTH_RULE_NAME..."
az eventhubs eventhub authorization-rule create --resource-group $RG_NAME --namespace-name $IOT_HUB_EVENTHUB_NS_NAME --eventhub-name $IOT_HUB_EVENTHUB_NAME --name $IOT_HUB_EVENTHUB_AUTH_RULE_NAME --rights Manage Listen Send --output none
IOT_HUB_EVENT_HUB_CONNECTION_STR=$(az eventhubs eventhub authorization-rule keys list --resource-group $RG_NAME --namespace-name $IOT_HUB_EVENTHUB_NS_NAME --eventhub-name $IOT_HUB_EVENTHUB_NAME --name $IOT_HUB_EVENTHUB_AUTH_RULE_NAME --query "primaryConnectionString" | tr -d '"')
echo "Creating IoT Hub custom endpoint $IOT_HUB_ROUTE_ENDPOINT_NAME..."
az iot hub routing-endpoint create --resource-group $RG_NAME --hub-name $IOT_HUB_NAME --endpoint-name $IOT_HUB_ROUTE_ENDPOINT_NAME --endpoint-type eventhub --endpoint-resource-group $RG_NAME --endpoint-subscription-id $SUBSCRIPTION_ID -c $IOT_HUB_EVENT_HUB_CONNECTION_STR --output none
echo "Creating IoT Hub route $IOT_HUB_ROUTE_NAME..."
az iot hub route create -g $RG_NAME --hub-name $IOT_HUB_NAME --endpoint-name $IOT_HUB_ROUTE_ENDPOINT_NAME --source-type DeviceMessages --route-name $IOT_HUB_ROUTE_NAME --output none

#setting up azure functions

SIGNALR_APP_NAME="${APP_PREFIX}signalr"
SIGNALR_API_IDENT="http://${SIGNALR_APP_NAME}"
SIGNALR_APP=$(az ad app create --display-name $SIGNALR_APP_NAME --identifier-uris $SIGNALR_API_IDENT)
SIGNALR_APP_APP_ID=$(echo $SIGNALR_APP | jq -r '.appId')
DEFAULT_SCOPE=$(az ad app show --id $SIGNALR_APP_APP_ID | jq '.oauth2Permissions[0].isEnabled = false' | jq -r '.oauth2Permissions')
az ad app update --id $SIGNALR_APP_APP_ID --set oauth2Permissions="$DEFAULT_SCOPE"
az ad app update --id $SIGNALR_APP_APP_ID --set oauth2Permissions=@signalr_oauth2-permissions.json

DEVICE_SERVICE_APP_NAME="${APP_PREFIX}deviceservice"
DEVICE_SERVICES_API_IDENT="http://${DEVICE_SERVICE_APP_NAME}"
DEVICE_SERVICES_APP=$(az ad app create --display-name $DEVICE_SERVICE_APP_NAME --identifier-uris $DEVICE_SERVICES_API_IDENT)
DEVICE_SERVICES_APP_APP_ID=$(echo $DEVICE_SERVICES_APP | jq -r '.appId')
DEFAULT_SCOPE=$(az ad app show --id $DEVICE_SERVICES_APP_APP_ID | jq '.oauth2Permissions[0].isEnabled = false' | jq -r '.oauth2Permissions')
az ad app update --id $DEVICE_SERVICES_APP_APP_ID --set oauth2Permissions="$DEFAULT_SCOPE"
az ad app update --id $DEVICE_SERVICES_APP_APP_ID --set oauth2Permissions=@device_services_oauth2-permissions.json

DASHBOARD_APP_NAME="${APP_PREFIX}dashboard"
DASHBOARD_API_IDENT="http://${DASHBOARD_APP_NAME}"
DASHBOARD_APP=$(az ad app create --display-name $DASHBOARD_APP_NAME --identifier-uris $DASHBOARD_API_IDENT)
DASHBOARD_APP_APP_ID=$(echo $DASHBOARD_APP | jq -r '.appId')

az ad app permission add --id $DASHBOARD_APP_APP_ID --api $SIGNALR_APP_APP_ID --api-permissions ef198d40-45e1-42c6-bee5-3496b5317711=Scope
az ad app permission add --id $DASHBOARD_APP_APP_ID --api $DEVICE_SERVICES_APP_APP_ID --api-permissions 6e003432-e10d-4e1e-bad9-741c104188b5=Scope

az ad app permission admin-consent --id $DASHBOARD_APP_APP_ID

FUNC_WEBSOCKET_NAME="${APP_PREFIX}funcsignalr"
FUNC_WEBSOCKET_STORAGE_NAME="${APP_PREFIX}stor"
echo "Creating function app for telemetry streaming with name $FUNC_WEBSOCKET_NAME..."
az storage account create -n $FUNC_WEBSOCKET_STORAGE_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2" --output none
az functionapp create -g RG_NAME --consumption-plan-location $RG_LOC -n FUNC_WEBSOCKET_NAME --os-type Windows --runtime dotnet --storage-account $FUNC_WEBSOCKET_STORAGE_NAME
az functionapp config appsettings set -n $FUNC_WEBSOCKET_NAME -g $RG_NAME --settings "DeviceUserDBConnectionString=$COSMOS_DB_CONNECTION_STR IoTDemonstratorServiceConnect=$IOT_HUB_CONNECTION_STR"
SIGNALR_FUNC_HOST_NAME=$(az functionapp show -g $RG_NAME -n $FUNC_WEBSOCKET_NAME --query "defaultHostName" | tr -d '"')

FUNC_DEVICE_SERVICES_NAME="${APP_PREFIX}funcdeviceservices"
FUNC_DEVICE_SERVICES_STORAGE_NAME="${APP_PREFIX}stor2"
echo "Creating function app for device services with name $FUNC_DEVICE_SERVICES_NAME..."
az storage account create -n $FUNC_DEVICE_SERVICES_STORAGE_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2" --output none
az functionapp create -g RG_NAME --consumption-plan-location $RG_LOC -n FUNC_DEVICE_SERVICES_NAME --os-type Windows --runtime dotnet --storage-account $FUNC_DEVICE_SERVICES_STORAGE_NAME
az functionapp config appsettings set -n $FUNC_DEVICE_SERVICES_NAME -g $RG_NAME --settings "DeviceUserDBConnectionString=$COSMOS_DB_CONNECTION_STR IoTDemonstratorIoTHubConnection=hub1temp AzureSignalRConnectionString=$SIGNALR_CONNECTION_STR"
FUNC_DEVICE_SERVICES_HOST_NAME=$(az functionapp show -g $RG_NAME -n $FUNC_DEVICE_SERVICES_NAME --query "defaultHostName" | tr -d '"')

cd ../../scripts/
./ad.sh