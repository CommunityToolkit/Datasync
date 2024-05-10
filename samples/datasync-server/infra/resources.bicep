targetScope = 'resourceGroup'

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The name of the App Service to create.')
param appServiceName string

@description('The name of the App Service Plan to create.')
param appServicePlanName string

@description('The name of the SQL Server to create.')
param sqlServerName string

@description('The name of the SQL Database to create.')
param sqlDatabaseName string

@description('The SQL Server administrator password.')
param sqlAdminUsername string

@secure()
@description('The SQL Server administrator password.')
param sqlAdminPassword string

@description('The list of tags to apply to all resources.')
param tags object = {}

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: sqlAdminPassword
  }

  resource firewall 'firewallRules' = {
    name: 'AllowAllAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: sqlDatabaseName
  parent: sqlServer
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 1073741824
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    capacity: 1
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  tags: union(tags, { 
    'azd-service-name': 'backend' 
    'hidden-related:${appServicePlan.id}': 'empty'
  })
  properties: {
    httpsOnly: true
    serverFarmId: appServicePlan.id
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }

  resource configLogs 'config' = {
    name: 'logs'
    properties: {
      applicationLogs: { fileSystem: { level: 'Verbose' } }
      detailedErrorMessages: { enabled: true }
      failedRequestsTracing: { enabled: true }
      httpLogs: { fileSystem: { retentionInMb: 35, retentionInDays: 3, enabled: true } }
    }
  }

  resource connectionStrings 'config' = {
    name: 'connectionstrings'
    properties: {
      DefaultConnection: {
        value: 'Data Source=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};User Id=${sqlServer.properties.administratorLogin};Password=${sqlAdminPassword};'
        type: 'SQLAzure'
      }
    }
  }
}

output SERVICE_ENDPOINT string = 'https://${appService.properties.defaultHostName}'
