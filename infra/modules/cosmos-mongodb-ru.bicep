targetScope = 'resourceGroup'

@minLength(1)
@description('The name of the test container to create')
param collectionName string = 'movies'

@minLength(1)
@description('The name of the test database to create')
param databaseName string = 'unittests'

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@allowed(['3.2', '3.6', '4.0', '4.2'])
@description('Specifies the MongoDB server version to use.')
param mongoVersion string = '4.2'

@description('The name of the Mongo Server to create.')
param serverName string

@description('The list of tags to apply to all resources.')
param tags object = {}

/*********************************************************************************/

/*
** Note that this implements the serverless (RU) model.  If you use the dedicated (vCore)
** deployment model, it acts much more like MongoDB Community Edition.
**
** The use of the serverless model means you have to manually add composite keys for just
** about everything.  See TestCommon MongoDBContext for how to do this.
*/

resource account 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
  name: toLower(serverName)
  location: location
  kind: 'MongoDB'
  tags: tags
  properties: {
    apiProperties: {
      serverVersion: mongoVersion
    }
    capabilities: [
      {
        name: 'DisableRateLimitingResponses'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: false
    locations: [
      {
        locationName: location
        isZoneRedundant: false
      }
    ]
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2022-05-15' = {
  parent: account
  name: databaseName
  tags: tags
  properties: {
    resource: {
      id: databaseName
    }
    options: {
      throughput: 400
    }
  }
}

resource collection 'Microsoft.DocumentDb/databaseAccounts/mongodbDatabases/collections@2022-05-15' = {
  parent: database
  name: collectionName
  tags: tags
  properties: {
    resource: {
      id: collectionName
      shardKey: {
        rating: 'Hash'
      }
      indexes: [
        {
          key: {
            keys: [
              '_id'
            ]
          }
        }
        {
          key: {
            keys: [
              '$**'
            ]
          }
        }
      ]
    }
  }
}

/*********************************************************************************/

#disable-next-line outputs-should-not-contain-secrets
output MONGODB_CONNECTIONSTRING string = account.listConnectionStrings().connectionStrings[1].connectionString
