# Serverless IoT Demo Project

# Architecture

# Setup
To setup the whole environment several steps are required. To ease the process you could checkout the scripts folder and run the files "predeploy.sh" and "deployment.sh". Be careful with the predeploy script as it install additional dependencies onto you machine.

The deployment script creates resources inside you Azure subscription an sets a lot (currently not all) of the neccessary settings for you. To give you a better understanding on what steps are required, the followig descriptions leads you through the manual setup process. Next to the steps you can do from the Azure portal I add the possible CLI commands as well.

1. Setup an Azure Resource Group either in the portal or from the CLI.

```
az storage account create -n $STORAGE_ACC_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2"
```

2. Create a storage account to host your static page contents from the GenericDashboard. The naming is up to you. To enable the "static page" mode on the storage account, go click the "Static Website" menu item in your storage account settings. If you like to do all the above mentioned steps on the Azure CLI checkout the following code snipped.

```
az storage account create -n $STORAGE_ACC_NAME -g $RG_NAME -l $RG_LOC --sku Standard_LRS --kind "StorageV2"

#enabling static website on blob storage
az storage blob service-properties update --account-name $STORAGE_ACC_NAME --static-website --index-document index.html
```