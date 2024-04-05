targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
#disable-next-line no-unused-params
param clientIpAddress string?

@description('The name of the database to create')
param databaseName string = 'unittests'

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The administrator username for the database')
#disable-next-line no-unused-params
param administratorUsername string?

@secure()
@description('The administrator password for the database')
#disable-next-line no-unused-params
param administratorPassword string?

var containerName = 'Movies'
var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
    name: 'cosmos-${resourceToken}'
    location: location
    kind: 'GlobalDocumentDB'
    properties: {
        consistencyPolicy: {
            defaultConsistencyLevel: 'Session'
        }
        locations: [
            {
                locationName: location
                failoverPriority: 0
                isZoneRedundant: false
            }
        ]
        databaseAccountOfferType: 'Standard'
        enableAutomaticFailover: false
        disableKeyBasedMetadataWriteAccess: true
    }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-02-15-preview' = {
    name: databaseName
    parent: account
    properties: {
        resource: {
            id: databaseName
        }
    }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
    name: containerName
    parent: database
    properties: {
        resource: {
            id: containerName
            partitionKey: {
                paths: [ '/id' ]
                kind: 'Hash'
            }
            indexingPolicy: {
                indexingMode: 'consistent'
                includedPaths: [
                    { path: '/*' }
                ]
                excludedPaths: [
                    { path: '/_etag/?' }
                ]
                compositeIndexes: [
                    [
                        { path: '/UpdatedAt', order: 'ascending' }
                        { path: '/Id', order: 'ascending' }
                    ]
                ]
            }
            defaultTtl: 86400
        }
        options: {
            throughput: 400
        }
    }
}


#disable-next-line outputs-should-not-contain-secrets
output connectionString string = account.listConnectionStrings().connectionStrings[0].connectionString
