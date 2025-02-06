targetScope = 'resourceGroup'

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The IP address of the place running the tests')
param clientIpAddress string?

@description('Id of the user or app to assign application roles')
#disable-next-line no-unused-params
param principalId string = ''

@description('The resource token to use in constructing all service names')
param resourceToken string

@description('The service name (in azure.yaml) to use for code deployment')
param serviceName string = 'todoservice'

@description('Optional - the SQL Server administrator password. If not provided, the username will be \'appadmin\'.')
param sqlAdminUsername string = 'appadmin'

@secure()
@description('Optional - SQL Server administrator password.  If not provided, a random password will be generated.')
param sqlAdminPassword string = newGuid()

@description('The list of tags to apply to all resources.')
param tags object = {}

/*********************************************************************************/

var appServicePlanName = 'asp-${resourceToken}'
var appServiceName = 'web-${resourceToken}'
var azsqlServerName = 'sql-${resourceToken}'
var cosmosServerName = 'cosmos-${resourceToken}'
var pgsqlServerName = 'pgsql-${resourceToken}'
var mysqlServerName = 'mysql-${resourceToken}'
var mongoServerName = 'mongo-${resourceToken}'
var mongoaciServerName = 'mongoaci-${resourceToken}'

var testDatabaseName = 'unittests'
var cosmosContainerName = 'Movies'

var clientIpFirewallRules = clientIpAddress != null
  ? [
      { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
      {
        endIpAddress: parseCidr('${clientIpAddress!}/32').lastUsable
        startIpAddress: parseCidr('${clientIpAddress!}/32').firstUsable
      }
    ]
  : [
      { endIpAddress: '255.255.255.255', startIpAddress: '0.0.0.0' }
    ]

/*********************************************************************************/

module azuresql './modules/azuresql.bicep' = {
    name: 'azsql-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        databaseName: testDatabaseName
        firewallRules: clientIpFirewallRules
        sqlServerName: azsqlServerName
        sqlAdminUsername: sqlAdminUsername
        sqlAdminPassword: sqlAdminPassword
    }
}

module pgsql './modules/postgresql.bicep' = {
    name: 'pgsql-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        databaseName: testDatabaseName
        firewallRules: clientIpFirewallRules
        sqlServerName: pgsqlServerName
        sqlAdminUsername: sqlAdminUsername
        sqlAdminPassword: sqlAdminPassword
    }
}

module mysql './modules/mysql.bicep' = {
    name: 'mysql-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        databaseName: testDatabaseName
        firewallRules: clientIpFirewallRules
        sqlServerName: mysqlServerName
        sqlAdminUsername: sqlAdminUsername
        sqlAdminPassword: sqlAdminPassword
    }
}

module cosmos './modules/cosmos.bicep' = {
    name: 'cosmos-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        databaseName: testDatabaseName
        containerName: cosmosContainerName
        serverName: cosmosServerName
    }
}

module mongodb './modules/cosmos-mongodb.bicep' = {
    name: 'mongo-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        serverName: mongoServerName
        administratorPassword: sqlAdminPassword
        administratorUsername: sqlAdminUsername
    }
}

module mongoaci './modules/aci-mongodb.bicep' = {
    name: 'mongoaci-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        serverName: mongoaciServerName
        administratorPassword: sqlAdminPassword

    }
}

module app_service './modules/appservice.bicep' = {
    name: 'appsvc-deployment-${resourceToken}'
    params: {
        location: location
        tags: tags
        serviceName: serviceName
        sqlServerName: azsqlServerName
        appServicePlanName: appServicePlanName
        appServiceName: appServiceName
        sqlAdminUsername: sqlAdminUsername
        sqlAdminPassword: sqlAdminPassword
    }
    dependsOn: [
        azuresql
    ]
}

/*********************************************************************************/

output AZSQL_CONNECTIONSTRING string = azuresql.outputs.AZSQL_CONNECTIONSTRING
output COSMOS_CONNECTIONSTRING string = cosmos.outputs.COSMOS_CONNECTIONSTRING
output MONGO_CONNECTIONSTRING string = mongodb.outputs.MONGO_CONNECTIONSTRING
output MONGOACI_CONNECTIONSTRING string = mongoaci.outputs.MONGO_CONNECTIONSTRING
output MYSQL_CONNECTIONSTRING string = mysql.outputs.MYSQL_CONNECTIONSTRING
output PGSQL_CONNECTIONSTRING string = pgsql.outputs.PGSQL_CONNECTIONSTRING
output SERVICE_ENDPOINT string = app_service.outputs.SERVICE_ENDPOINT
