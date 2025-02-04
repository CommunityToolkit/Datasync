targetScope = 'resourceGroup'

@minLength(1)
@description('The name of the test container to create')
param containerName string = 'Movies'

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

var compositeIndices = [
  [
    { path: '/BestPictureWinner', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/BestPictureWinner', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Duration', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Duration', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Rating', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Rating', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/ReleaseDate', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/ReleaseDate', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Title', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Title', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/UpdatedAt', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/UpdatedAt', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Year', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Year', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Year', order: 'ascending' }
    { path: '/Title', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Year', order: 'descending' }
    { path: '/Title', order: 'ascending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Year', order: 'ascending' }
    { path: '/Title', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
  [
    { path: '/Year', order: 'descending' }
    { path: '/Title', order: 'descending' }
    { path: '/id', order: 'ascending' }
  ]
]

/*********************************************************************************/

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
  name: containerName
  tags: tags
  properties: {
    resource: {
      id: containerName
      shardKey: {
        _id: 'Hash'
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
