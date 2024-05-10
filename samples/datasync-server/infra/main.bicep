// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Optional - the name of the App Service to create. If not provided, a unique name will be generated.')
param appServiceName string = ''

@description('Optional - the name of the App Service Plan to create. If not provided, a unique name will be generated.')
param appServicePlanName string = ''

@description('Optional - the name of the Resource Group to create. If not provided, a unique name will be generated.')
param resourceGroupName string = ''

@description('Optional - the name of the SQL Server to create. If not provided, a unique name will be generated.')
param sqlServerName string = ''

@description('Optional - the name of the SQL Database to create. If not provided, a unique name will be generated.')
param sqlDatabaseName string = ''

@description('Optional - the SQL Server administrator password. If not provided, the username will be \'appadmin\'.')
param sqlAdminUsername string = 'appadmin'

@secure()
@description('Optional - SQL Server administrator password.  If not provided, a random password will be generated.')
param sqlAdminPassword string = newGuid()

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

resource resourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources './resources.bicep' = {
  name: 'resources'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    appServiceName: !empty(appServiceName) ? appServiceName : 'app-${resourceToken}'
    appServicePlanName: !empty(appServicePlanName) ? appServicePlanName : 'asp-${resourceToken}'
    sqlServerName: !empty(sqlServerName) ? sqlServerName : 'sql-${resourceToken}'
    sqlDatabaseName: !empty(sqlDatabaseName) ? sqlDatabaseName : 'TodoDb'
    sqlAdminUsername: !empty(sqlAdminUsername) ? sqlAdminUsername : 'appadmin'
    sqlAdminPassword: sqlAdminPassword
  }
}

output SERVICE_ENDPOINT string = resources.outputs.SERVICE_ENDPOINT

