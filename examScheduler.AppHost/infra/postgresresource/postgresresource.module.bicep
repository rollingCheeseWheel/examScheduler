@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource postgresresource 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: take('postgresresource-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
    }
    availabilityZone: '1'
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    storage: {
      storageSizeGB: 32
    }
    version: '16'
  }
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  tags: {
    'aspire-resource-name': 'postgresresource'
  }
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: postgresresource
}

resource postgresdb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'postgresdb'
  parent: postgresresource
}

output connectionString string = 'Host=${postgresresource.properties.fullyQualifiedDomainName}'

output name string = postgresresource.name

output hostName string = postgresresource.properties.fullyQualifiedDomainName