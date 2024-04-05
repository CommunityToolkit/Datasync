targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
param clientIpAddress string?

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The administrator username for the database')
#disable-next-line no-unused-params
param administratorUsername string?

@secure()
@description('The administrator password for the database')
param administratorPassword string?

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))

resource cosmos_account 'Microsoft.DocumentDB/databaseAccounts@2023-09-15' = {
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
    }
}

resource cosmos_database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-09-15' = {
    name: 'unittests'
    parent: cosmos_account
    properties: {
        resource: {
            id: 'unittests'
        }
    }
}

resource cosmos_container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
    name: 'Movies'
    parent: cosmos_database
    properties: {
        resource: {
            id: 'Movies'
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
        }
    }
}


#disable-next-line outputs-should-not-contain-secrets
output connectionString string = cosmos_account.listConnectionStrings().connectionStrings[0].connectionString
