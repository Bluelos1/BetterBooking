targetScope = 'resourceGroup'

@description('Short lowercase environment name, for example test or prod.')
param environmentName string = 'test'

@description('Lowercase workload prefix used in resource names. Use letters, numbers, and hyphens only.')
param namePrefix string = 'betterbooking-test'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Tags applied to all resources.')
param tags object = {}

@description('OIDC issuer/authority used by backend JWT validation and frontend sign-in.')
param authenticationAuthority string

@description('Backend API token audience expected by JWT validation.')
param authenticationAudience string

@description('Frontend OIDC application client id.')
param frontendAuthClientId string

@description('Frontend OIDC scopes, including openid/profile/email and the backend API scope.')
param frontendAuthScopes string

@secure()
@description('Optional frontend OIDC confidential-client secret. Leave empty for public clients or when frontendAuthClientSecretUri is supplied.')
param frontendAuthClientSecret string = ''

@description('Optional existing Key Vault secret URI for BETTERBOOKING_AUTH_CLIENT_SECRET. Leave empty for public clients or when frontendAuthClientSecret is supplied.')
param frontendAuthClientSecretUri string = ''

@secure()
@description('High-entropy value used to encrypt frontend HttpOnly session cookies.')
param frontendAuthCookieSecret string

@description('PostgreSQL administrator login. Use only for server administration; create a least-privileged app user before PROD.')
param postgresqlAdminLogin string = 'bbadmin'

@secure()
@description('PostgreSQL administrator password. Supply at deployment time only.')
param postgresqlAdminPassword string

@description('PostgreSQL database name used by the application.')
param postgresqlDatabaseName string = 'betterbooking'

@description('PostgreSQL major version.')
@allowed([
  '16'
  '17'
])
param postgresqlVersion string = '17'

@description('PostgreSQL compute SKU.')
param postgresqlSkuName string = 'Standard_B1ms'

@description('PostgreSQL SKU tier.')
@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
param postgresqlSkuTier string = 'Burstable'

@description('PostgreSQL storage size in GiB.')
@minValue(32)
param postgresqlStorageSizeGb int = 32

@description('PostgreSQL firewall rules. Use explicit CIDR/IP ranges for TEST.')
param postgresqlFirewallRules array = []

@description('Temporarily allow Azure services to reach PostgreSQL by adding the 0.0.0.0 firewall rule. Prefer private networking before PROD.')
param allowAzureServicesToPostgreSql bool = false

@description('Linux App Service Plan SKU.')
param appServicePlanSkuName string = 'B1'

@description('Linux App Service Plan SKU tier.')
param appServicePlanSkuTier string = 'Basic'

@description('Linux runtime stack for the backend API App Service.')
param apiLinuxFxVersion string = 'DOTNETCORE|10.0'

@description('Linux runtime stack for the frontend App Service.')
param webLinuxFxVersion string = 'NODE|22-lts'

@description('ASP.NET Core environment name for the backend API. Use Test for TEST and Production for PROD.')
@allowed([
  'Test'
  'Production'
])
param aspNetCoreEnvironment string = environmentName == 'prod' ? 'Production' : 'Test'

var uniqueSuffix = uniqueString(resourceGroup().id, environmentName, namePrefix)
var resourceTags = union(tags, {
  environment: environmentName
  workload: 'betterbooking'
})
var hasFrontendAuthClientSecret = !empty(frontendAuthClientSecret)
var hasFrontendAuthClientSecretUri = !empty(frontendAuthClientSecretUri)
var logAnalyticsName = take('${namePrefix}-log-${uniqueSuffix}', 63)
var appInsightsName = take('${namePrefix}-appi-${uniqueSuffix}', 255)
var keyVaultName = take('${namePrefix}-kv-${uniqueSuffix}', 24)
var appServicePlanName = take('${namePrefix}-asp-${uniqueSuffix}', 40)
var apiAppName = take('${namePrefix}-api-${uniqueSuffix}', 60)
var webAppName = take('${namePrefix}-web-${uniqueSuffix}', 60)
var postgresqlServerName = take('${namePrefix}-pg-${uniqueSuffix}', 63)
var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6'
var databaseConnectionString = 'Host=${postgresqlServer.name}.postgres.database.azure.com;Port=5432;Database=${postgresqlDatabaseName};Username=${postgresqlAdminLogin};Password=${postgresqlAdminPassword};SSL Mode=Require;Trust Server Certificate=false;Include Error Detail=false'
var apiBaseUrl = 'https://${apiApp.properties.defaultHostName}'
var webBaseUrl = 'https://${webApp.properties.defaultHostName}'
var frontendAuthClientSecretAppSettingUri = hasFrontendAuthClientSecret ? frontendAuthClientSecretSecret!.properties.secretUri : frontendAuthClientSecretUri
var frontendClientSecretAppSetting = hasFrontendAuthClientSecret || hasFrontendAuthClientSecretUri ? {
  BETTERBOOKING_AUTH_CLIENT_SECRET: '@Microsoft.KeyVault(SecretUri=${frontendAuthClientSecretAppSettingUri})'
} : {}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: resourceTags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: resourceTags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: resourceTags
  properties: {
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enablePurgeProtection: true
    enableSoftDelete: true
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: postgresqlServerName
  location: location
  tags: resourceTags
  sku: {
    name: postgresqlSkuName
    tier: postgresqlSkuTier
  }
  properties: {
    version: postgresqlVersion
    administratorLogin: postgresqlAdminLogin
    administratorLoginPassword: postgresqlAdminPassword
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
      storageSizeGB: postgresqlStorageSizeGb
    }
  }
}

resource postgresqlExtensionsConfiguration 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-01-preview' = {
  parent: postgresqlServer
  name: 'azure.extensions'
  properties: {
    value: 'btree_gist'
    source: 'user-override'
  }
}

resource postgresqlDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresqlServer
  name: postgresqlDatabaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource postgresqlFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = [for rule in postgresqlFirewallRules: {
  parent: postgresqlServer
  name: rule.name
  properties: {
    startIpAddress: rule.startIpAddress
    endIpAddress: rule.endIpAddress
  }
}]

resource postgresqlAllowAzureServicesRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = if (allowAzureServicesToPostgreSql) {
  parent: postgresqlServer
  name: 'allow-azure-services'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource databaseConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ApplicationDatabaseConnectionString'
  properties: {
    value: databaseConnectionString
  }
}

resource applicationInsightsConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ApplicationInsightsConnectionString'
  properties: {
    value: appInsights.properties.ConnectionString
  }
}

resource frontendAuthCookieSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FrontendAuthCookieSecret'
  properties: {
    value: frontendAuthCookieSecret
  }
}

resource frontendAuthClientSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (hasFrontendAuthClientSecret) {
  parent: keyVault
  name: 'FrontendAuthClientSecret'
  properties: {
    value: frontendAuthClientSecret
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  tags: resourceTags
  sku: {
    name: appServicePlanSkuName
    tier: appServicePlanSkuTier
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux'
  tags: resourceTags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      http20Enabled: true
      linuxFxVersion: apiLinuxFxVersion
      minTlsVersion: '1.2'
    }
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  tags: resourceTags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      alwaysOn: true
      appCommandLine: 'node server.js'
      ftpsState: 'FtpsOnly'
      http20Enabled: true
      linuxFxVersion: webLinuxFxVersion
      minTlsVersion: '1.2'
    }
  }
}

resource apiKeyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiApp.id, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource webKeyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.id, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: apiApp
  name: 'appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: aspNetCoreEnvironment
    Authentication__Authority: authenticationAuthority
    Authentication__Audience: authenticationAudience
    Cors__AllowedOrigins__0: webBaseUrl
    ConnectionStrings__ApplicationDatabase: '@Microsoft.KeyVault(SecretUri=${databaseConnectionStringSecret.properties.secretUri})'
    APPLICATIONINSIGHTS_CONNECTION_STRING: '@Microsoft.KeyVault(SecretUri=${applicationInsightsConnectionStringSecret.properties.secretUri})'
    WEBSITE_RUN_FROM_PACKAGE: '1'
  }
  dependsOn: [
    apiKeyVaultSecretsUserRole
  ]
}

resource webAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: webApp
  name: 'appsettings'
  properties: union({
    NODE_ENV: 'production'
    BETTERBOOKING_API_BASE_URL: apiBaseUrl
    BETTERBOOKING_WEB_BASE_URL: webBaseUrl
    BETTERBOOKING_AUTH_ISSUER: authenticationAuthority
    BETTERBOOKING_AUTH_CLIENT_ID: frontendAuthClientId
    BETTERBOOKING_AUTH_SCOPES: frontendAuthScopes
    BETTERBOOKING_AUTH_COOKIE_SECRET: '@Microsoft.KeyVault(SecretUri=${frontendAuthCookieSecretSecret.properties.secretUri})'
    WEBSITE_RUN_FROM_PACKAGE: '1'
  }, frontendClientSecretAppSetting)
  dependsOn: [
    webKeyVaultSecretsUserRole
  ]
}

output apiAppName string = apiApp.name
output apiBaseUrl string = apiBaseUrl
output webAppName string = webApp.name
output webBaseUrl string = webBaseUrl
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output postgresqlServerName string = postgresqlServer.name
output postgresqlHost string = '${postgresqlServer.name}.postgres.database.azure.com'
output apiOutboundIpAddresses string = apiApp.properties.outboundIpAddresses
output webOutboundIpAddresses string = webApp.properties.outboundIpAddresses
output applicationInsightsName string = appInsights.name
