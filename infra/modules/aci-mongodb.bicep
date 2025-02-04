targetScope = 'resourceGroup'

@secure()
@description('The password for the administrator')
param administratorPassword string

@description('The username for the administrator')
param administratorUsername string = 'tester'

@description('The image URI to use.')
param image string = 'mongo'

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@description('The port # to expose in the Docker image')
param port int = 27017

@description('The name of the Mongo Server to create.')
param serverName string

@description('The list of tags to apply to all resources.')
param tags object = {}

/*********************************************************************************/

resource containerGroup 'Microsoft.ContainerInstance/containerGroups@2023-05-01' = {
  name: toLower(serverName)
  location: location
  tags: tags
  properties: {
    containers: [
      {
        name: toLower(serverName)
        properties: {
          image: image
          environmentVariables: [
            { name: 'MONGO_INITDB_ROOT_USERNAME', value: administratorUsername }
            { name: 'MONGO_INITDB_ROOT_PASSWORD', secureValue: administratorPassword }
          ]
          ports: [
            {
              port: port
              protocol: 'TCP'
            }
          ]
          resources: {
            requests: {
              cpu: 2
              memoryInGB: 2
            }
          }
        }
      }
    ]
    osType: 'Linux'
    restartPolicy: 'Always'
    ipAddress: {
      type: 'Public'
      ports: [
        {
          port: port
          protocol: 'TCP'
        }
      ]
    }
  }
}

/*********************************************************************************/

#disable-next-line outputs-should-not-contain-secrets
output MONGO_CONNECTIONSTRING string = 'mongodb://${administratorUsername}:${administratorPassword}@${containerGroup.properties.ipAddress.ip}:27017/'
