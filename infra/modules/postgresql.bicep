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

resource pgsql_server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
    name: sqlServerName
    location: location
    tags: tags
    sku: {
        name: 'Standard_B1ms'
        tier: 'Burstable'
    }
    properties: {
        administratorLogin: sqlAdminUsername
        administratorLoginPassword: sqlAdminPassword
        createMode: 'Default'
        authConfig: {
            activeDirectoryAuth: 'Disabled'
            passwordAuth: 'Enabled'
        }
        backup: {
            backupRetentionDays: 7
            geoRedundantBackup: 'Disabled'
        }
        highAvailability: {
            mode: 'Disabled'
        }
        storage: {
            storageSizeGB: 32
            autoGrow: 'Disabled'
        }
        version: '15'
    }

    resource fw 'firewallRules' = [ for (fwRule, idx) in firewallRules : {
        name: 'fw${idx}'
        properties: {
            startIpAddress: fwRule.startIpAddress
            endIpAddress: fwRule.endIpAddress
        }
    }]
}

resource pgsql_database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
    name: databaseName
    parent: pgsql_server
    properties: {
        charset: 'UTF8'
        collation: 'en_US.utf8'
    }
}

/*********************************************************************************/

#disable-next-line outputs-should-not-contain-secrets
output PGSQL_CONNECTIONSTRING string = 'Host=${pgsql_server.properties.fullyQualifiedDomainName};Database=${pgsql_database.name};Username=${pgsql_server.properties.administratorLogin};Password=${sqlAdminPassword}'

/*********************************************************************************/

type FirewallRule = {
  startIpAddress: string
  endIpAddress: string
}
