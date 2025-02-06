targetScope = 'resourceGroup'

@secure()
@description('The password for the administrator')
param administratorPassword string

@description('The username for the administrator')
param administratorUsername string = 'tester'

@description('The list of firewall rules to install')
param firewallRules FirewallRule[] = [
  { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
]

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@description('The name of the Mongo Server to create.')
param serverName string

@description('The list of tags to apply to all resources.')
param tags object = {}

@description('The tier to use for compute')
@allowed([ 'Free', 'M10', 'M20', 'M25', 'M30', 'M40', 'M50', 'M60', 'M80', 'M200', 'M200-Autoscale'])
param tier string = 'M10'

/*********************************************************************************/

resource cluster 'Microsoft.DocumentDB/mongoClusters@2024-07-01' = {
  name: toLower(serverName)
  location: location
  tags: tags
  properties: {
    administrator: {
      userName: administratorUsername
      password: administratorPassword
    }
    compute: { tier: tier }
    highAvailability: {
      targetMode: 'Disabled'
    }
    publicNetworkAccess: 'Enabled'
    serverVersion: '7.0'
    sharding: {
      shardCount: 1
    }
    storage: { sizeGb: 32 }
  }
}

resource mongoFirewallRule 'Microsoft.DocumentDB/mongoClusters/firewallRules@2024-07-01' = [
  for (fwRule, index) in firewallRules: {
    name: fwRule.?name ?? 'rule-${index}'
    parent: cluster
    properties: {
      startIpAddress: fwRule.startIpAddress
      endIpAddress: fwRule.endIpAddress
    }
  }
]

/*********************************************************************************/

#disable-next-line outputs-should-not-contain-secrets
output MONGO_CONNECTIONSTRING string = replace(replace(cluster.listConnectionStrings().connectionStrings[0].connectionString, '<user>', administratorUsername), '<password>', administratorPassword)

/*********************************************************************************/

type FirewallRule = {
  name: string?
  startIpAddress: string
  endIpAddress: string
}
