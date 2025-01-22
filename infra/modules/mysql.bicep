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

resource mysql_server 'Microsoft.DBforMySQL/flexibleServers@2024-10-01-preview' = {
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
        version: '8.0.21'
    }

    resource fw 'firewallRules@2023-12-30' = [ for (fwRule, idx) in firewallRules : {
        name: 'fw${idx}'
        properties: {
            startIpAddress: fwRule.startIpAddress
            endIpAddress: fwRule.endIpAddress
        }
    }]
}

resource mysql_database 'Microsoft.DBforMySQL/flexibleServers/databases@2023-12-30' = {
    name: databaseName
    parent: mysql_server
    properties: {
        charset: 'ascii'
        collation: 'ascii_general_ci'
    }
}

/*********************************************************************************/

#disable-next-line outputs-should-not-contain-secrets
output MYSQL_CONNECTIONSTRING string = 'server=${mysql_server.properties.fullyQualifiedDomainName};database=${mysql_database.name};user=${mysql_server.properties.administratorLogin};password=${sqlAdminPassword}'

/*********************************************************************************/

type FirewallRule = {
  startIpAddress: string
  endIpAddress: string
}
