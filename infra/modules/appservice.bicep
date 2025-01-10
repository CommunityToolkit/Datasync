targetScope = 'resourceGroup'

@minLength(1)
@description('The name of the App Service Plan resource')
param appServicePlanName string

@minLength(1)
@description('The name of the App Service resource')
param appServiceName string

@minLength(1)
@description('The name of the test database to create')
param databaseName string = 'tododb'

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@description('The name of the deployment in azure.yaml')
param serviceName string = 'todoservice'

@description('The name of the SQL Server to create.')
param sqlServerName string

@description('Optional - the SQL Server administrator password. If not provided, the username will be \'appadmin\'.')
param sqlAdminUsername string = 'appadmin'

@secure()
@description('Optional - SQL Server administrator password.  If not provided, a random password will be generated.')
param sqlAdminPassword string = newGuid()

@description('The list of tags to apply to all resources.')
param tags object = {}

/*********************************************************************************/

resource azsql_server 'Microsoft.Sql/servers@2024-05-01-preview' existing = {
  name: sqlServerName
}

resource sqldb 'Microsoft.Sql/servers/databases@2024-05-01-preview' = {
  name: databaseName
  parent: azsql_server
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

resource appsvc_plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    capacity: 1
  }
}

resource app_service 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  tags: union(tags, {
    'azd-service-name': serviceName
    'hidden-related:${appsvc_plan.id}': 'empty'
  })
  properties: {
    httpsOnly: true
    serverFarmId: appsvc_plan.id
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
        value: 'Data Source=tcp:${azsql_server.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqldb.name};User Id=${sqlAdminUsername};Password=${sqlAdminPassword};'
        type: 'SQLAzure'
      }
    }
  }
}

/*********************************************************************************/

output SERVICE_ENDPOINT string = 'https://${app_service.properties.defaultHostName}'
