targetScope = 'resourceGroup'

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The name of the App Service to create.')
param appServiceName string

@description('The name of the App Service Plan to create.')
param appServicePlanName string

@description('The list of tags to apply to all resources.')
param tags object = {}

@description('The name for the CosmosDB account')
param accountName string

@description('The name for the database')
param databaseName string

@description('The name for the container')
param containerName string

resource account 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
  name: toLower(accountName)
  kind: 'GlobalDocumentDB'
  location: location
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'}
    locations: [
      {
        locationName: location
      }
    ]
    databaseAccountOfferType: 'Standard'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
    options: {
      throughput: 1000
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  parent: database
  name: containerName
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: [
          '/myPartitionKey'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/_etag/?'
          }
        ]
        compositeIndexes: [
          [
            {
              path: '/name'
              order: 'ascending'
            }
            {
              path: '/age'
              order: 'descending'
            }
          ]
        ]
      }
    }
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
        value: account.listConnectionStrings().connectionStrings[0].connectionString
        type: 'DocDb'
      }
    }
  }
}

output SERVICE_ENDPOINT string = 'https://${appService.properties.defaultHostName}'
