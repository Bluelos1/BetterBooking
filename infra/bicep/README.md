# Azure Infrastructure

`main.bicep` provisions the current Azure baseline for one BetterBooking environment. Use separate resource groups or subscriptions for TEST and PROD.

## Current Resources

- Linux App Service Plan.
- Linux App Service for the ASP.NET Core API.
- Linux App Service for the Next.js frontend.
- Azure Database for PostgreSQL Flexible Server and application database.
- Azure Key Vault with RBAC enabled.
- Key Vault secrets for the PostgreSQL connection string, Application Insights connection string, frontend cookie secret, and optional frontend OIDC client secret.
- System-assigned managed identities for both App Services.
- Key Vault Secrets User role assignments for both App Service identities.
- Log Analytics workspace and workspace-based Application Insights.
- Optional PostgreSQL firewall rules.

## Required Deployment Inputs

Do not commit real secret values in parameter files, docs, appsettings, or examples.

- `authenticationAuthority`: TEST OIDC issuer/authority.
- `authenticationAudience`: TEST backend API token audience.
- `frontendAuthClientId`: TEST frontend OIDC client id.
- `frontendAuthScopes`: OIDC scopes plus the backend API scope.
- `frontendAuthCookieSecret`: secure, high-entropy frontend session cookie encryption key.
- `postgresqlAdminPassword`: secure PostgreSQL administrator password.
- `postgresqlFirewallRules` or `allowAzureServicesToPostgreSql`: temporary TEST network access until private networking is added.
- `frontendAuthClientSecret` or `frontendAuthClientSecretUri`: only when the OIDC provider requires a confidential frontend client.

`main.test.parameters.example.json` intentionally contains no secret values and is not directly deployable without supplying secure parameters.

## TEST Deployment

The repository includes `.github/workflows/azure-deploy.yml` for Azure deployments. On pushes to `main`, it builds, tests, packages, deploys or updates TEST infrastructure, applies EF Core migrations, deploys the API and frontend App Services, and runs smoke checks.

Create or select a TEST resource group first:

```sh
az group create --name <test-resource-group> --location <azure-region>
```

Deploy the template from CI using protected secret variables, or from a secured admin shell. Pass secure values from your secret store; do not paste real values into `main.test.parameters.example.json`.

```sh
az deployment group create \
  --resource-group <test-resource-group> \
  --template-file infra/bicep/main.bicep \
  --parameters @infra/bicep/main.test.parameters.example.json \
  --parameters postgresqlAdminPassword="$BETTERBOOKING_TEST_POSTGRES_ADMIN_PASSWORD" \
  --parameters frontendAuthCookieSecret="$BETTERBOOKING_TEST_AUTH_COOKIE_SECRET"
```

If the TEST OIDC frontend application is confidential, also pass `frontendAuthClientSecret` from a protected secret variable or set `frontendAuthClientSecretUri` to an existing Key Vault secret URI that the web app identity can read.

After deployment, use the `webBaseUrl` output as the OIDC redirect origin and configure the identity provider callback URL as:

```text
<webBaseUrl>/api/auth/callback
```

Use the `apiBaseUrl` output for smoke tests and frontend backend calls.

The frontend App Service is configured to run the Next.js standalone artifact with `node server.js`. Deployment packages should include `.next/standalone` at the zip root and `.next/static` under the zip root.

## Validation

Run this before opening a PR or deploying from a new machine:

```sh
az bicep build --file infra/bicep/main.bicep --stdout
```

Then run a what-if against the TEST resource group:

```sh
az deployment group what-if \
  --resource-group <test-resource-group> \
  --template-file infra/bicep/main.bicep \
  --parameters @infra/bicep/main.test.parameters.example.json \
  --parameters postgresqlAdminPassword="$BETTERBOOKING_TEST_POSTGRES_ADMIN_PASSWORD" \
  --parameters frontendAuthCookieSecret="$BETTERBOOKING_TEST_AUTH_COOKIE_SECRET"
```

## PROD Gaps

Before PROD, add or explicitly decide on these items:

- Private networking for PostgreSQL and Key Vault access.
- PROD App Service deployment slots and swap-based rollback.
- Least-privileged PostgreSQL application user instead of runtime use of the server administrator login.
- Azure Front Door Premium with WAF.
- Storage, Redis, and Service Bus only when features require them.
- Backup, retention, alerting, and incident runbook review.

Do not create Azure resources manually as the source of truth. Infrastructure must be reproducible from code.
