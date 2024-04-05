targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
param clientIpAddress string?

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The administrator username for the databases')
param administratorUsername string

@secure()
@description('The administrator password for the databases')
param administratorPassword string

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))
var cidr = clientIpAddress != null ? parseCidr('${clientIpAddress}/32') : null

resource server 'Microsoft.Sql/servers@2023-05-01-preview' = {
    name: 'azsql-${resourceToken}'
    location: location
    properties: {
        administratorLogin: administratorUsername
        administratorLoginPassword: administratorPassword
        version: '12.0'
        publicNetworkAccess: 'Enabled'
    }

    resource allowAzureIps 'firewallRules' = {
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

resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
    name: 'unittests'
    location: location
    parent: server
    sku: {
        name: 'Basic'
        tier: 'Basic'
        capacity: 5
    }
    properties: {
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        maxSizeBytes: 104857600
    }
}

#disable-next-line outputs-should-not-contain-secrets
output connectionString string = 'Data Source=tcp:${server.properties.fullyQualifiedDomainName},1433;Initial Catalog=${database.name};User Id=${administratorUsername}@${server.properties.fullyQualifiedDomainName};Password=${administratorPassword};Encrypt=True;TrustServerCertificate=False'
