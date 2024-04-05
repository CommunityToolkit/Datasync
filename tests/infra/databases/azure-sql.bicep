targetScope = 'resourceGroup'

@description('The IP address of the place running the tests')
param clientIpAddress string?

@description('The name of the database to create')
param databaseName string = 'unittests'

@minLength(1)
@description('Primary location for all resources')
param location string

@description('The administrator username for the databases')
param administratorUsername string

@secure()
@description('The administrator password for the databases')
param administratorPassword string

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().name, location))

var clientIpFirewallRules = clientIpAddress != null ? [
    { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
    { endIpAddress: parseCidr('${clientIpAddress!}/32').lastUsable, startIpAddress: parseCidr('${clientIpAddress!}/32').firstUsable }
] : [
    { endIpAddress: '255.255.255.255', startIpAddress: '0.0.0.0' }
]

resource server 'Microsoft.Sql/servers@2023-08-01-preview' = {
    name: 'azsql-${resourceToken}'
    location: location
    properties: {
        administratorLogin: administratorUsername
        administratorLoginPassword: administratorPassword
    }

    resource fw 'firewallRules' = [ for fwRule in clientIpFirewallRules : {
        name: '${fwRule.startIpAddress}-${fwRule.endIpAddress}'
        properties: {
            startIpAddress: fwRule.startIpAddress
            endIpAddress: fwRule.endIpAddress
        }
    }]
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
    name: databaseName
    parent: server
    location: location
    sku: {
        name: 'Basic'
        tier: 'Basic'
    }
    properties: {
        collation: 'SQL_Latin1_General_CP1_CI_AS'
    }
}

#disable-next-line outputs-should-not-contain-secrets
output connectionString string = 'Data Source=tcp:${server.properties.fullyQualifiedDomainName},1433;Initial Catalog=${database.name};User Id=${administratorUsername}@${server.properties.fullyQualifiedDomainName};Password=${administratorPassword};Encrypt=True;TrustServerCertificate=False'
