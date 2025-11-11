@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param database_outputs_name string

param principalType string

param principalId string

param principalName string

resource database 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: database_outputs_name
}

resource database_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  name: principalId
  properties: {
    principalName: principalName
    principalType: principalType
  }
  parent: database
}