# TEST and PROD Deployment Flow

## Environment Separation

- TEST and PROD must use separate Azure resources.
- PROD secrets must exist only in the PROD Key Vault.
- TEST must not connect to PROD databases, storage accounts, Redis, Service Bus, or Key Vault.
- Environment-specific settings should come from hosting configuration and Key Vault references; add Azure App Configuration only when the application needs centralized dynamic configuration.
- TEST and PROD must use separate identity-provider applications and token audiences.
- Frontend session cookie secrets must be unique per environment.
- Required runtime settings are tracked in `docs/deployment/runtime-configuration.md`.

## TEST Deployment

- Merge to the selected integration branch deploys to TEST automatically.
- Run build, tests, formatting checks, dependency scan, secret scan, SAST, and IaC scan first.
- Deploy or update TEST infrastructure from `infra/bicep/main.bicep` with TEST-only parameters and protected CI secrets.
- Use the Bicep `webBaseUrl` output to configure the TEST identity-provider redirect URI: `<webBaseUrl>/api/auth/callback`.
- Keep TEST Auth0 tenant/application, issuer, client id, API audience, client secret, and frontend cookie secret separate from PROD values.
- Apply reviewed EF Core migrations to the TEST PostgreSQL database before routing smoke traffic to the new API build.
- Deploy the backend and frontend artifacts to the TEST App Services created by Bicep.
- Run smoke tests after deployment.
- Keep OpenAPI enabled in TEST for validation and client generation.

`/.github/workflows/azure-deploy.yml` implements the current automated TEST path for pushes to `main` and manual TEST runs. It builds and tests backend and frontend, publishes zip artifacts, deploys Bicep, applies EF Core migrations, deploys both App Services, and runs smoke checks against `/health/live`, `/health/ready`, and the frontend URL.

Required GitHub environment secrets for `test`:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `POSTGRESQL_ADMIN_PASSWORD`
- `FRONTEND_AUTH_COOKIE_SECRET`
- `FRONTEND_AUTH_CLIENT_SECRET`, only when the TEST OIDC frontend client is confidential
- `POSTGRESQL_MIGRATION_CONNECTION_STRING`, only when migrations should use a dedicated connection string instead of the provisioned PostgreSQL admin credentials

Required GitHub environment variables for `test`:

- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`
- `AZURE_NAME_PREFIX`
- `AUTHENTICATION_AUTHORITY`
- `AUTHENTICATION_AUDIENCE`
- `FRONTEND_AUTH_CLIENT_ID`
- `FRONTEND_AUTH_SCOPES`

For Auth0 use `openid profile email`; `AUTHENTICATION_AUDIENCE` contains the separate Auth0 API Identifier.

Optional GitHub environment variables for `test`:

- `ALLOW_AZURE_SERVICES_TO_POSTGRESQL`, default `false`
- `POSTGRESQL_FIREWALL_RULES_JSON`, default `[]`
- `FRONTEND_AUTH_CLIENT_SECRET_URI`
- `POSTGRESQL_ADMIN_LOGIN`, default `bbadmin`
- `POSTGRESQL_DATABASE_NAME`, default `betterbooking`
- `AZURE_API_APP_NAME`, `AZURE_WEB_APP_NAME`, `AZURE_KEY_VAULT_NAME`, `AZURE_API_BASE_URL`, `AZURE_WEB_BASE_URL`, and `POSTGRESQL_HOST` when running the workflow with `deploy_infrastructure=false`

Minimum TEST smoke path:

- API `/health/live` returns healthy.
- API `/health/ready` returns healthy and includes the database check when the connection string is configured.
- Frontend can start sign-in and complete `/api/auth/callback`.
- Authenticated user can create a listing, publish it, find it publicly, request a reservation, and cancel their reservation.

Current TEST networking note:

- `infra/bicep/main.bicep` supports explicit PostgreSQL firewall rules and a temporary `allowAzureServicesToPostgreSql` switch.
- Prefer explicit TEST-only firewall rules when feasible.
- Do not use the temporary Azure-services firewall rule for PROD; add private networking first.

## PROD Deployment

- PROD requires manual approval.
- Deploy artifacts to a PROD staging slot first.
- Use PROD-only infrastructure, Key Vault, OIDC applications, token audiences, databases, and secrets.
- Run database migration scripts only after review.
- Run smoke tests against the staging slot.
- Warm up the application.
- Swap the staging slot to production only after successful checks.
- Keep OpenAPI disabled in PROD unless explicitly secured.

The current Bicep template is a TEST-ready baseline. Before using it for PROD, complete the PROD gaps documented in `infra/bicep/README.md`.

The deploy workflow has a PROD safety gate. It will not deploy PROD unless the `prod` GitHub environment has `PROD_DEPLOYMENT_ENABLED=true` and `AZURE_DEPLOYMENT_SLOT_NAME` configured. Keep `PROD_DEPLOYMENT_ENABLED` unset until PROD slots, approval rules, private networking, backup/restore, monitoring, and rollback procedures are ready.

Required additional GitHub environment variables for `prod` when PROD deployment is enabled:

- `PROD_DEPLOYMENT_ENABLED=true`
- `AZURE_DEPLOYMENT_SLOT_NAME`, for example `staging`

For PROD manual runs, leave `swap_production=false` to deploy and smoke-test the staging slot only. Set `swap_production=true` only when the staging smoke checks passed, the slot settings are correct, and the release is approved for production traffic.

## Rollback

- Prefer App Service slot swap rollback for fast recovery.
- Keep previous artifacts available for redeployment.
- Avoid destructive database migrations in the same release as dependent code.
- Use expand, deploy, migrate/backfill, contract for risky schema changes.
