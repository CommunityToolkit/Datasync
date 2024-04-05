targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
param clientIpAddress string?

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@description('The administrator username for the databases')
param administratorUsername string = 'testadmin'

@secure()
@description('The administrator password for the databases')
param administratorPassword string = newGuid()

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))

// Azure SQL
module azuresql './databases/azure-sql.bicep' = {
    name: 'deploy-azuresql-${resourceToken}'
    params: {
        administratorUsername: administratorUsername
        administratorPassword: administratorPassword
        clientIpAddress: clientIpAddress
        location: location
    }
}

#disable-next-line outputs-should-not-contain-secrets
output AZSQL_CONNECTIONSTRING string = azuresql.outputs.connectionString

// PostgreSQL
module postgresql './databases/postgresql.bicep' = {
    name: 'deploy-postgresql-${resourceToken}'
    params: {
        administratorUsername: administratorUsername
        administratorPassword: administratorPassword
        clientIpAddress: clientIpAddress
        location: location
    }
}

#disable-next-line outputs-should-not-contain-secrets
output PGSQL_CONNECTIONSTRING string = postgresql.outputs.connectionString

// Cosmos
module cosmos './databases/cosmos.bicep' = {
    name: 'deploy-cosmos-${resourceToken}'
    params: {
        administratorUsername: administratorUsername
        administratorPassword: administratorPassword
        clientIpAddress: clientIpAddress
        location: location
    }
}

#disable-next-line outputs-should-not-contain-secrets
output COSMOS_CONNECTIONSTRING string = cosmos.outputs.connectionString
