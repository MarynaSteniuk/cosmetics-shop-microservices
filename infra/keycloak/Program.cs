using Cosmetics.Infra;
using Pulumi;
using Keycloak = Pulumi.Keycloak;

// Лаб. 09, частина 3: Infrastructure as Code для Keycloak (запущеного через Aspire AppHost).
// Запуск: pulumi login --local; pulumi stack init dev; pulumi config set --secret ...; pulumi up
return await Deployment.RunAsync(() =>
{
    const string realmName = "DevRealmDrivers";

    var config = new Config();
    // Aspire 13 запускає Keycloak лише на HTTPS (контейнерний порт 8443 → https://localhost:8080)
    var keycloakUrl = config.Get("keycloakUrl") ?? "https://localhost:8080";

    var provider = new Keycloak.Provider("keycloak-local", new Keycloak.ProviderArgs
    {
        Url = keycloakUrl,
        Realm = "master",
        ClientId = "admin-cli",
        Username = config.Get("keycloakAdmin") ?? "admin",
        Password = config.RequireSecret("keycloakPassword")
    });

    CustomResourceOptions Opts(params Resource[] dependsOn) => new()
    {
        Provider = provider,
        DependsOn = dependsOn
    };

    // ---------- Realm ----------
    var realm = new Keycloak.Realm("dev-realm", new Keycloak.RealmArgs
    {
        RealmName = realmName,
        DisplayName = "Cosmetics shop (Lab 09)",
        Enabled = true,
        LoginWithEmailAllowed = true,
        ResetPasswordAllowed = true,
        AccessTokenLifespan = "15m",
        PasswordPolicy = "length(8) and digits(1) and upperCase(1) and lowerCase(1) and specialChars(1)"
    }, Opts());

    // Factory pattern: усі OpenID clients (resource servers і споживачі) — у KeycloakClientFactory
    var clients = new KeycloakClientFactory(realm.Id, provider);

    // ---------- Resource servers: client + authorization scopes + resource + client roles ----------
    var servers = new Dictionary<string, ResourceServer>();

    foreach (var definition in PermissionMatrix.ResourceServers)
    {
        var client = clients.CreateResourceServer(definition);

        var scopes = new Dictionary<string, Keycloak.OpenId.ClientAuthorizationScope>();
        var clientRoles = new Dictionary<string, Keycloak.Role>();

        foreach (var permission in definition.ScopeRoles.Keys)
        {
            var key = ResourceKey(definition.ClientId, permission);

            scopes[permission] = new Keycloak.OpenId.ClientAuthorizationScope($"{key}-scope",
                new Keycloak.OpenId.ClientAuthorizationScopeArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = permission,
                    DisplayName = permission
                }, Opts());

            // Клієнтська роль з тією ж назвою потрапляє в resource_access.{client}.roles звичайного access token
            clientRoles[permission] = new Keycloak.Role($"{key}-role", new Keycloak.RoleArgs
            {
                RealmId = realm.Id,
                ClientId = client.Id,
                Name = permission,
                Description = $"Permission {permission}"
            }, Opts());
        }

        var resource = new Keycloak.OpenId.ClientAuthorizationResource($"{definition.ClientId}-resource",
            new Keycloak.OpenId.ClientAuthorizationResourceArgs
            {
                RealmId = realm.Id,
                ResourceServerId = client.ResourceServerId,
                Name = definition.ResourceName,
                DisplayName = definition.DisplayName,
                Type = $"urn:{definition.ClientId}:resources:{definition.ResourceName}",
                Uris = { definition.Uri },
                Scopes = Strings(definition.ScopeRoles.Keys)
            }, Opts([.. scopes.Values]));

        servers[definition.ClientId] = new ResourceServer(definition, client, resource, scopes, clientRoles);
    }

    // ---------- Realm roles admin / manager / user / guest: composite з клієнтських ролей за матрицею ----------
    var realmRoles = new Dictionary<string, Keycloak.Role>();

    foreach (var roleName in PermissionMatrix.RealmRoles)
    {
        var composites = servers.Values
            .SelectMany(server => server.Definition.ScopeRoles
                .Where(entry => entry.Value.Contains(roleName))
                .Select(entry => server.ClientRoles[entry.Key].Id));

        realmRoles[roleName] = new Keycloak.Role($"realm-role-{roleName}", new Keycloak.RoleArgs
        {
            RealmId = realm.Id,
            Name = roleName,
            Description = $"Cosmetics shop role '{roleName}' (Lab 09 permission matrix)",
            CompositeRoles = Ids(composites)
        }, Opts());
    }

    // ---------- Keycloak Authorization: role policies + scope permissions ----------
    foreach (var server in servers.Values)
    {
        var policies = new Dictionary<string, Keycloak.OpenId.ClientRolePolicy>();

        foreach (var roleName in PermissionMatrix.RealmRoles)
        {
            if (!server.Definition.ScopeRoles.Values.Any(roles => roles.Contains(roleName)))
                continue;

            policies[roleName] = new Keycloak.OpenId.ClientRolePolicy(
                $"{server.Definition.ClientId}-policy-{roleName}", new Keycloak.OpenId.ClientRolePolicyArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = server.Client.ResourceServerId,
                    Name = $"{roleName}-role-policy",
                    Description = $"Grants access to users with realm role '{roleName}'",
                    Type = "role",
                    Logic = "POSITIVE",
                    DecisionStrategy = "UNANIMOUS",
                    Roles =
                    {
                        new Keycloak.OpenId.Inputs.ClientRolePolicyRoleArgs
                        {
                            Id = realmRoles[roleName].Id,
                            Required = true
                        }
                    }
                }, Opts());
        }

        foreach (var (permission, allowedRoles) in server.Definition.ScopeRoles)
        {
            _ = new Keycloak.OpenId.ClientAuthorizationPermission(
                $"{ResourceKey(server.Definition.ClientId, permission)}-permission",
                new Keycloak.OpenId.ClientAuthorizationPermissionArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = server.Client.ResourceServerId,
                    Name = $"{permission} permission",
                    Description = $"{permission}: {string.Join(", ", allowedRoles)}",
                    Type = "scope",
                    DecisionStrategy = "AFFIRMATIVE",
                    Resources = { server.Resource.Id },
                    Scopes = { server.Scopes[permission].Id },
                    Policies = Ids(allowedRoles.Select(role => policies[role].Id))
                }, Opts());
        }
    }

    // ---------- Client scopes orders_mgmt / products_mgmt / payments_mgmt з audience mapper ----------
    var clientScopes = new List<Keycloak.OpenId.ClientScope>();

    foreach (var server in servers.Values)
    {
        var clientScope = new Keycloak.OpenId.ClientScope($"{server.Definition.ClientScopeName}-client-scope",
            new Keycloak.OpenId.ClientScopeArgs
            {
                RealmId = realm.Id,
                Name = server.Definition.ClientScopeName,
                Description = $"Access to {server.Definition.DisplayName}: adds aud={server.Definition.ClientId}",
                IncludeInTokenScope = true
            }, Opts());

        clientScope
            .WithAudienceMapper(server.Definition, server.Client, realm.Id, provider)
            .WithPermissionsMapper(server.Definition, server.Client, realm.Id, provider);

        clientScopes.Add(clientScope);
    }

    // ---------- Клієнти-споживачі: factory + extension method WithDefaultScopes ----------
    var postman = clients
        .CreateAuthorizationCodeClient("postman-client", "postman", "Postman (Authorization Code + Password grant for testing)",
            config.RequireSecret("postmanSecret"), ["https://oauth.pstmn.io/v1/callback"], allowPasswordGrant: true)
        .WithDefaultScopes("postman", realm.Id, clientScopes, provider);

    var swagger = clients
        .CreatePkceClient("swagger-client", "swagger", "Swagger UI (Authorization Code + PKCE)",
            ["https://localhost:7206/swagger/oauth2-redirect.html", "https://localhost:7098/swagger/oauth2-redirect.html"],
            ["https://localhost:7206", "https://localhost:7098"])
        .WithDefaultScopes("swagger", realm.Id, clientScopes, provider);

    var aggregator = clients
        .CreateClientCredentialsClient("aggregator-service-client", "aggregator-service", "Aggregator (Client Credentials)",
            config.RequireSecret("aggregatorSecret"))
        .WithDefaultScopes("aggregator-service", realm.Id, clientScopes, provider);

    // Service account Aggregator отримує лише orders:read і products:read
    foreach (var (resourceServer, permission) in PermissionMatrix.AggregatorPermissions)
    {
        _ = new Keycloak.OpenId.ClientServiceAccountRole($"aggregator-{ResourceKey(resourceServer, permission)}",
            new Keycloak.OpenId.ClientServiceAccountRoleArgs
            {
                RealmId = realm.Id,
                ServiceAccountUserId = aggregator.ServiceAccountUserId,
                ClientId = servers[resourceServer].Client.Id,
                Role = servers[resourceServer].ClientRoles[permission].Name
            }, Opts());
    }

    // ---------- Тестові користувачі з тимчасовим паролем (при першому вході Keycloak вимагає новий) ----------
    var initialPassword = config.RequireSecret("userInitialPassword");

    foreach (var testUser in PermissionMatrix.Users)
    {
        var user = new Keycloak.User($"user-{testUser.Username}", new Keycloak.UserArgs
        {
            RealmId = realm.Id,
            Username = testUser.Username,
            Enabled = true,
            Email = testUser.Email,
            EmailVerified = true,
            FirstName = testUser.FirstName,
            LastName = testUser.LastName,
            InitialPassword = new Keycloak.Inputs.UserInitialPasswordArgs
            {
                Value = initialPassword,
                Temporary = true
            }
        }, Opts());

        _ = new Keycloak.UserRoles($"user-{testUser.Username}-roles", new Keycloak.UserRolesArgs
        {
            RealmId = realm.Id,
            UserId = user.Id,
            RoleIds = { realmRoles[testUser.RealmRole].Id },
            Exhaustive = false
        }, Opts());
    }

    var realmUrl = $"{keycloakUrl}/realms/{realmName}";

    // Outputs за вимогами п. 3.6: RealmUrl, AdminConsoleUrl, ClientIds, ClientSecrets
    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = realmUrl,
        ["AdminConsoleUrl"] = $"{keycloakUrl}/admin/master/console/#/{realmName}",
        ["TokenEndpoint"] = $"{realmUrl}/protocol/openid-connect/token",
        ["ClientIds"] = new[] { "postman", "swagger", "aggregator-service", "orders-api", "products-api", "payments-api" },
        ["ClientSecrets"] = Output.CreateSecret(Output.Tuple(postman.ClientSecret, aggregator.ClientSecret)
            .Apply(secrets => new Dictionary<string, object?>
            {
                ["postman"] = secrets.Item1,
                ["aggregator-service"] = secrets.Item2
            })),
        ["Realm"] = realm.RealmName,
        ["RealmRoles"] = PermissionMatrix.RealmRoles.ToArray(),
        ["ResourceServers"] = servers.Keys.ToArray(),
        ["Users"] = PermissionMatrix.Users.Select(u => $"{u.Username} ({u.RealmRole})").ToArray()
    };
});

static string ResourceKey(string clientId, string permission) => $"{clientId}-{permission.Replace(':', '-')}";

static InputList<string> Strings(IEnumerable<string> values)
{
    var list = new InputList<string>();
    foreach (var value in values)
        list.Add(value);
    return list;
}

static InputList<string> Ids(IEnumerable<Output<string>> ids)
{
    var list = new InputList<string>();
    foreach (var id in ids)
        list.Add(id);
    return list;
}
