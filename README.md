# Serverless IoT Demo Project

# Architecture
![serverless-iot-architecture](/assets/serverless-iot-architecture.png)

# Setup
To setup the whole environment several steps are required. To ease the process you could checkout the scripts folder and run the files "predeploy.sh" and "deployment.sh". Be careful with the predeploy script as it install additional dependencies onto you machine.

The deployment script creates resources inside you Azure subscription an sets a lot (currently not all) of the neccessary settings for you. To give you a better understanding on what steps are required, the followig descriptions leads you through the manual setup process. Next to the steps you can execute from the Azure portal I have added the commands you can run from the az CLI.

## Setup an Azure Resource Group either in the portal or from the CLI.

```
az storage account create -n $STORAGE_ACC_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2"
```

## Create a storage account to host your static page contents from the GenericDashboard. 
Create a new storage account from the azure portal. The naming is up to you. To enable the "static page" mode on the storage account, go click the "Static Website" menu item in your storage account settings and enable the static website. If you like to do all the above mentioned steps on the Azure CLI checkout the following code snipped. In both cases note down the primary endpoint of your static website. In the portal you can find in directly in your static website settings. If you are using the CLI you an use the command below to query for the endpoint after setting the storage account to host a static site.

```
STORAGE_ACC_NAME="dashboard"
az storage account create -n $STORAGE_ACC_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2"

#enabling static website on blob storage
az storage blob service-properties update --account-name $STORAGE_ACC_NAME --static-website --index-document index.html
DASHBOARD_URL=$(az storage account show --name $STORAGE_ACC_NAME --query 'primaryEndpoints.web' | tr -d '"' | awk -F[/:] '{print $4}')
```

## Create an Azure API Management. 
You might choose any tier you want. In my demo I have used the serverless consumption tier. As part of this step we are just setting up the APIM the majority of setup is done in a later step. The easiest way to create an APIM manually is via the portal. If you want to do the setup via the CLI you can referr to the code below.

```
#creating API Management
APIM_NAME="$APIM_NAME"
APIM_PUBLISHER_EMAIL="youremail@example.com"
APIM_PUBLISHER_ORG="yourorgname"

az apim create -g $RG_NAME -l $RG_LOC --sku-name Consumption --publisher-email $APIM_PUBLISHER_EMAIL --publisher-name $APIM_PUBLISHER_ORG --name $APIM_NAME
```

## Create Azure Front Door
Here we create your Azure Front Door environment. If you are doing that from the portal you can use the Front Door designer to setup your backends and routing. For this demo you need a routing for both the API Management and the GenericDashboard on you blob storage (hosted as static website).

* After you have created the Front Door resource in your resource group, go to the Front Door designer.
  * You should see that the Frontends/domain panel should already contain an entry. The backend pools and routing rules need to be filled in the upcoming steps.
  * First create the backend pool for your APIM. To do that, open up the "add backend pool forms" by clicking on the blue plus symbol at the top right corner of your backend pool pane.
    * Set a name. For the demo I recommend "apim"
    * Add a backend
      * Choose "API Management" as Backend host type
      * Select the subscription were your APIM is hosted in
      * The backend host name is pre filled (no change needed)
      * The backend host header should be set to your APIM URI
      * HTTP Port: 80
      * HTTPS Port: 443
      * Priority: 1
      * Weight: 50
    * Leave the rest to the defaults
  * Now we have to create the backend pool for our dashboard application. Create another backend pool by clicking the add symbol.
    * Set a name. For the demo I recommend "dashboard"
    * Add a backend
      * Choose "Custom host" as Backend host type
      * In the field for your backend host name you need to enter the host name of your static website. Please fill in the URL you have copied in step 2. Please remove the "https://" and enter just the plain URI.
      * The backend host header: Please fill in the URL you have copied in step 2. Please remove the "https://" and enter just the plain URI.
      * HTTP Port: 80
      * HTTPS Port: 443
      * Priority: 1
      * Weight: 50
    * Leave the rest to the defaults
  * Not that we have created the backend pools it is time to setup our routing rules for the APIM and dashboard requests.
    * Create a new rule by clicking the blue add symbol in the frotdoor designer routing rules pane. In the following I will explain the settings I have setup for the demo environment and both my routing rules.
      * The process is straight forward. Select a name for your rule. As accepted protocols you select HTTPS.
        * Patterns to match: For the APIM you should keep the default of "/*". For the dashboard rule you use "/dashboard/*".
        * Route type: Forward
        * Backend pool: Select the right backend pool for your routing rule. The dashboard rule gets the dashboard routing rule.
        * Forwarding protocol should be HTTPS only
        * All other settings are disabled
```
FD_NAME="nameofyourfrontdoor"
FD_LB_SETTINGS_NAME="nameofyourfrontdoorlbsettings"
FD_BACKEND_POOL_APIM_NAME="apim"
FD_BACKEND_POOL_DASHBOARD_NAME="dashboard"
FD_HOST="${FD_NAME}.azurefd.net"

#Create FrontDoors with name $FD_NAME
az network front-door create -g $RG_NAME --backend-address $FD_HOST --name $FD_NAME

#Create FrontDoors load balancing settings.
az network front-door load-balancing create --front-door-name $FD_NAME --name $FD_LB_SETTINGS_NAME --resource-group $RG_NAME --sample-size 4 --successful-samples-required 2

#Create FrontDoors probe settings.
az network front-door probe create --front-door-name $FD_NAME --interval 30 --name "probesettings" --path "/" --resource-group $RG_NAME --enabled Disabled

#Create FrontDoors backend pools.
az network front-door backend-pool create --address $APIM_GW_HOST --front-door-name $FD_NAME --load-balancing $FD_LB_SETTINGS_NAME --name $FD_BACKEND_POOL_APIM_NAME -g $RG_NAME --probe "probesettings" 
az network front-door backend-pool create --address $DASHBOARD_URL --front-door-name $FD_NAME --load-balancing $FD_LB_SETTINGS_NAME --name $FD_BACKEND_POOL_DASHBOARD_NAME -g $RG_NAME --probe "probesettings"

#Create FrontDoors routing rules.
az network front-door routing-rule create --front-door-name $FD_NAME --frontend-endpoints "DefaultFrontendEndpoint" --name "apimrule" --resource-group $RG_NAME --route-type Forward --backend-pool $FD_BACKEND_POOL_APIM_NAME --accepted-protocols Https
az network front-door routing-rule create --front-door-name $FD_NAME --frontend-endpoints "DefaultFrontendEndpoint" --name "dashboardrule" --resource-group $RG_NAME --route-type Forward --backend-pool $FD_BACKEND_POOL_DASHBOARD_NAME --accepted-protocols Https --patterns "/dashboard/*"

FRONT_DOOR_DASHBOARD_ENDPOINT="https://${FD_NAME}.azurefd.net/${FD_BACKEND_POOL_DASHBOARD_NAME}/"
```

## Create SignalR
Now we create our SignalrR resource. This is a pretty easy step. Go to the portal and search for Signalr. Choose the pricing tier (for demo purposes go with the free tier and a unit count of 1). The Service mode should be "Serverless". 

```
SIGNALR_NAME="signalrname"
#Creating signalr service with name $SIGNALR_NAME.
az signalr create -n $SIGNALR_NAME -g $RG_NAME --sku Standard_S1 --unit-count 1 --service-mode Serverless -l $RG_LOC
SIGNALR_CONNECTION_STR=$(az signalr key list -n $SIGNALR_NAME -g $RG_NAME --query primaryConnectionString -o tsv)
```

## Create CosmosDb

## Create IoT Hub

## Setting up functions

## Bringing it all together