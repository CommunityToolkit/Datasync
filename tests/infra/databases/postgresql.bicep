targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
param clientIpAddress string?

@description('The name of the database to create')
param databaseName string = 'unittests'

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The administrator username for the database')
param administratorUsername string

@secure()
@description('The administrator password for the database')
param administratorPassword string

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))
var clientIpFirewallRules = clientIpAddress != null ? [
    { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
    { endIpAddress: parseCidr('${clientIpAddress!}/32').lastUsable, startIpAddress: parseCidr('${clientIpAddress!}/32').firstUsable }
] : [
    { endIpAddress: '255.255.255.255', startIpAddress: '0.0.0.0' }
]


resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
    name: 'pgserver-${resourceToken}'
    location: location
    sku: {
        name: 'Standard_B1ms'
        tier: 'Burstable'
    }
    properties: {
        administratorLogin: administratorUsername
        administratorLoginPassword: administratorPassword
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

    resource fw 'firewallRules' = [ for (fwRule, idx) in clientIpFirewallRules : {
        name: 'fw${idx}'
        properties: {
            startIpAddress: fwRule.startIpAddress
            endIpAddress: fwRule.endIpAddress
        }
    }]
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
    name: databaseName
    parent: server
    properties: {
        charset: 'UTF8'
        collation: 'en_US.utf8'
    }
}

#disable-next-line outputs-should-not-contain-secrets
output connectionString string = 'Host=${server.properties.fullyQualifiedDomainName};Database=${database.name};Username=${administratorUsername};Password=${administratorPassword}'
