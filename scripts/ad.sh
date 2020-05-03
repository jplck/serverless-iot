#!/bin/bash

CALLBACK_AUTH_SUFFIX="/.auth/login/aad/callback"
SIGNALR_FULL_URL="https://$SIGNALR_FUNC_HOST_NAME"
DEVICE_SERVICES_FULL_URL="https://$FUNC_DEVICE_SERVICES_HOST_NAME"

echo "Creating Signalr App registration..."
SIGNALR_APP_NAME="${APP_PREFIX}signalr"
SIGNALR_API_IDENT="http://${SIGNALR_APP_NAME}"
SIGNALR_APP_APP_ID=$(az ad app create --display-name $SIGNALR_APP_NAME --reply-urls "$SIGNALR_FULL_URL" "${SIGNALR_FULL_URL}${CALLBACK_AUTH_SUFFIX}" --oauth2-allow-implicit-flow true --identifier-uris $SIGNALR_API_IDENT --query "appId" | tr -d '"')
echo "SIGNALR_APP_ID: ${SIGNALR_APP_APP_ID}"
sleep 30
SIGNALR_DEFAULT_SCOPE=$(az ad app show --id "$SIGNALR_APP_APP_ID" | jq '.oauth2Permissions[0].isEnabled = false' | jq -r '.oauth2Permissions')
echo "SIGNALR_DEFAULT_SCOPE: ${SIGNALR_DEFAULT_SCOPE}"
echo "Update signalr default scope -> set enabled = false"
az ad app update --id $SIGNALR_APP_APP_ID --set oauth2Permissions="$SIGNALR_DEFAULT_SCOPE"
echo "Update signalr scopes. Add manifest."
TEMP_PERMISSIONS_FILE="permissions_tmp.json"
SIGNALR_UUID=$(uuidgen)
cat signalr_oauth2-permissions.json | jq --arg uuid $SIGNALR_UUID '.oauth2Permissions[0].id = $uuid' | jq -r '.oauth2Permissions' > $TEMP_PERMISSIONS_FILE
az ad app update --id $SIGNALR_APP_APP_ID --set oauth2Permissions=@$TEMP_PERMISSIONS_FILE
echo "----------------------------------------------------------------------------------"
echo "----------------------------------------------------------------------------------"
echo "Creating Device Services App registration..."
DEVICE_SERVICE_APP_NAME="${APP_PREFIX}deviceservice"
DEVICE_SERVICES_API_IDENT="http://${DEVICE_SERVICE_APP_NAME}"
DEVICE_SERVICES_APP_APP_ID=$(az ad app create --display-name $DEVICE_SERVICE_APP_NAME --reply-urls "$DEVICE_SERVICES_FULL_URL" "${DEVICE_SERVICES_FULL_URL}${CALLBACK_AUTH_SUFFIX}" --oauth2-allow-implicit-flow true --identifier-uris $DEVICE_SERVICES_API_IDENT --query "appId" | tr -d '"')
echo "DEVICE_SERVICES_APP_APP_ID: ${DEVICE_SERVICES_APP_APP_ID}"
sleep 30
DEVICE_SERVICES_DEFAULT_SCOPE=$(az ad app show --id $DEVICE_SERVICES_APP_APP_ID | jq '.oauth2Permissions[0].isEnabled = false' | jq -r '.oauth2Permissions')
echo "DEVICE_SERVICES_DEFAULT_SCOPE: ${DEVICE_SERVICES_DEFAULT_SCOPE}"
echo "Update device services default scope -> set enabled = false"
az ad app update --id $DEVICE_SERVICES_APP_APP_ID --set oauth2Permissions="$DEVICE_SERVICES_DEFAULT_SCOPE"
echo "Update device services scopes. Add manifest."
DEVICE_SERVICES_UUID=$(uuidgen)
cat device_services_oauth2-permissions.json | jq --arg uuid $DEVICE_SERVICES_UUID '.oauth2Permissions[0].id = $uuid' | jq -r '.oauth2Permissions' > $TEMP_PERMISSIONS_FILE
az ad app update --id $DEVICE_SERVICES_APP_APP_ID --set oauth2Permissions=@$TEMP_PERMISSIONS_FILE

echo "Creating Dashboard registration..."
DASHBOARD_APP_NAME="${APP_PREFIX}dashboard"
DASHBOARD_API_IDENT="http://${DASHBOARD_APP_NAME}"
DASHBOARD_APP_APP_ID=$(az ad app create --display-name $DASHBOARD_APP_NAME --oauth2-allow-implicit-flow true --identifier-uris $DASHBOARD_API_IDENT --query "appId" | tr -d '"')
echo "DASHBOARD_APP_APP_ID: ${DASHBOARD_APP_APP_ID}"
sleep 60

echo "az ad app permission add --id $DASHBOARD_APP_APP_ID --api $SIGNALR_APP_APP_ID --api-permissions ${SIGNALR_UUID}=Scope"
echo "az ad app permission add --id $DASHBOARD_APP_APP_ID --api $DEVICE_SERVICES_APP_APP_ID --api-permissions ${DEVICE_SERVICES_UUID}=Scope"

az ad app permission add --id $DASHBOARD_APP_APP_ID --api $SIGNALR_APP_APP_ID --api-permissions "${SIGNALR_UUID}=Scope"
az ad app permission add --id $DASHBOARD_APP_APP_ID --api $DEVICE_SERVICES_APP_APP_ID --api-permissions "${DEVICE_SERVICES_UUID}=Scope"

az ad app permission admin-consent --id $DASHBOARD_APP_APP_ID
