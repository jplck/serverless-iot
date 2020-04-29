#!/bin/bash
echo "Setting up prerequisites..."
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
curl -sL https://deb.nodesource.com/setup_10.x | sudo -E bash -
sudo apt-get update
sudo apt-get install ca-certificates curl apt-transport-https lsb-release gnupg nodejs -y
curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc.gpg > /dev/null
AZ_REPO=$(lsb_release -cs) echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $AZ_REPO main" | sudo tee /etc/apt/sources.list.d/azure-cli.list
sudo apt-get update
sudo apt-get install azure-cli -y

#login to azure
az login

echo "Enter subscription id: "
read SUB_ID
az account select -s $SUB_ID