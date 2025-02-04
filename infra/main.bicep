targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@description('Optional - the SQL Server administrator password. If not provided, the username will be \'appadmin\'.')
param sqlAdminUsername string = 'appadmin'

@secure()
@description('Optional - SQL Server administrator password.  If not provided, a random password will be generated.')
param sqlAdminPassword string = newGuid()

/*********************************************************************************/

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

/*********************************************************************************/

resource rg 'Microsoft.Resources/resourceGroups@2024-07-01' = {
    name: 'rg-${environmentName}'
    location: location
    tags: tags
}

module resources './resources.bicep' = {
    name: 'resources'
    scope: rg
    params: {
      location: location
      tags: tags
      principalId: principalId
      resourceToken: resourceToken
      serviceName: 'todoservice'
      sqlAdminUsername: sqlAdminUsername
      sqlAdminPassword: sqlAdminPassword
    }
}

/*********************************************************************************/

output AZSQL_CONNECTION_STRING string = resources.outputs.AZSQL_CONNECTIONSTRING
output COSMOS_CONNECTION_STRING string = resources.outputs.COSMOS_CONNECTIONSTRING
output MONGO_CONNECTION_STRING string = resources.outputs.MONGO_CONNECTIONSTRING
output MONGOACI_CONNECTION_STRING string = resources.outputs.MONGOACI_CONNECTIONSTRING
output MYSQL_CONNECTION_STRING string = resources.outputs.MYSQL_CONNECTIONSTRING
output PGSQL_CONNECTION_STRING string = resources.outputs.PGSQL_CONNECTIONSTRING
output SERVICE_ENDPOINT string = resources.outputs.SERVICE_ENDPOINT
