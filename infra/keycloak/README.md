# infra/keycloak — Pulumi (C#) для Keycloak

Лабораторна 09, частина 3. Створює в Keycloak (запущеному через `Platform.AppHost`, https://localhost:8080 — Aspire 13 віддає Keycloak лише по HTTPS):

| Що | Значення |
|----|----------|
| Realm | `DevRealmDrivers` |
| Resource servers (Authorization Services, ENFORCING) | `orders-api`, `products-api`, `payments-api` |
| Authorization scopes / client roles | `orders:read/create/update/delete/approve`, `products:read/create/update/delete/publish`, `payments:read/process/refund` |
| Realm roles (composite) | `admin`, `manager`, `user`, `guest` |
| Policies / permissions | role policy на кожну роль + scope permission на кожен дозвіл (AFFIRMATIVE) |
| Client scopes з audience mapper і mapper claim `permissions` | `orders_mgmt`, `products_mgmt`, `payments_mgmt` |
| Клієнти | `postman` (confidential, Auth Code + password grant), `swagger` (public, PKCE S256), `aggregator-service` (Client Credentials) |
| Користувачі (тимчасовий пароль) | `alice` (admin), `bob` (manager), `carol` (user), `dave` (guest) |

Файли:
- `Program.cs` — realm, resource servers, ролі, policies/permissions, client scopes, users, outputs;
- `PermissionMatrix.cs` — матриця доступу (таблиці 3.3.1–3.3.3), ролі й тестові користувачі;
- `KeycloakClientFactory.cs` — factory pattern для clients: resource server, Authorization Code, PKCE, Client Credentials;
- `KeycloakClientExtensions.cs` — extension methods: `WithDefaultScopes`, `WithAudienceMapper`, `WithPermissionsMapper`;
- `ResourceServer.cs` — створені ресурси одного resource server.

## Запуск

```
cd infra/keycloak
set PULUMI_CONFIG_PASSPHRASE=lab09
pulumi login --local
pulumi stack init dev
pulumi config set keycloakUrl https://localhost:8080
pulumi config set --secret keycloakPassword <пароль адміністратора Keycloak>
pulumi config set --secret postmanSecret postman-kc-secret
pulumi config set --secret aggregatorSecret aggregator-kc-secret
pulumi config set --secret userInitialPassword Temp#2026Pass
pulumi preview
pulumi up --yes
pulumi stack output
```

Outputs: `RealmUrl`, `AdminConsoleUrl`, `TokenEndpoint`, `ClientIds`, `ClientSecrets` (секрет; показати: `pulumi stack output ClientSecrets --show-secrets`).

Видалити все: `pulumi destroy --yes`.
