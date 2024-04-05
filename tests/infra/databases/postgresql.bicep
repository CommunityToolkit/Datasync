targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
param clientIpAddress string?

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The administrator username for the database')
param administratorUsername string

@secure()
@description('The administrator password for the database')
param administratorPassword string

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))
var cidr = clientIpAddress != null ? parseCidr('${clientIpAddress}/32') : null

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
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

    resource AllowAzureIps 'firewallRules' = {
        name: 'AllowAllAzureIps'
        properties: {
            endIpAddress: '0.0.0.0'
            startIpAddress: '0.0.0.0'
        }
    }

    resource allowClientIp 'firewallRules' = if (clientIpAddress != null) {
        name: 'AllowClientIp'
        properties: {
            endIpAddress: cidr.lastUsable
            startIpAddress: cidr.firstUsable
        }
    }

    resource allowPublicAccess 'firewallRules' = if (clientIpAddress == null) {
        name: 'AllowPublicAccess'
        properties: {
            endIpAddress: '255.255.255.255'
            startIpAddress: '0.0.0.0'
        }
    }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
    name: 'unittests'
    parent: server
    properties: {
        charset: 'UTF8'
        collation: 'en_US.utf8'
    }
}

#disable-next-line outputs-should-not-contain-secrets
output connectionString string = 'Host=${server.properties.fullyQualifiedDomainName};Database=${database.name};Username=${administratorUsername};Password=${administratorPassword}'
