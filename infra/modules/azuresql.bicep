targetScope = 'resourceGroup'

@description('The list of firewall rules to install')
param firewallRules FirewallRule[] = [
  { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
]

@minLength(1)
@description('The name of the test database to create')
param databaseName string = 'unittests'

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

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

resource azsql_server 'Microsoft.Sql/servers@2024-05-01-preview' = {
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

  resource fw 'firewallRules' = [
    for fwRule in firewallRules: {
      name: '${fwRule.startIpAddress}-${fwRule.endIpAddress}'
      properties: {
        startIpAddress: fwRule.startIpAddress
        endIpAddress: fwRule.endIpAddress
      }
    }
  ]
}

resource azsql_database 'Microsoft.Sql/servers/databases@2024-05-01-preview' = {
    name: databaseName
    parent: azsql_server
    location: location
    tags: tags
    sku: {
        name: 'Basic'
        tier: 'Basic'
    }
    properties: {
        collation: 'SQL_Latin1_General_CP1_CI_AS'
    }
}

/*********************************************************************************/

#disable-next-line outputs-should-not-contain-secrets
output AZSQL_CONNECTIONSTRING string = 'Data Source=tcp:${azsql_server.properties.fullyQualifiedDomainName},1433;Initial Catalog=${azsql_database.name};User Id=${azsql_server.properties.administratorLogin}@${azsql_server.properties.fullyQualifiedDomainName};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False'

/*********************************************************************************/

type FirewallRule = {
  startIpAddress: string
  endIpAddress: string
}
