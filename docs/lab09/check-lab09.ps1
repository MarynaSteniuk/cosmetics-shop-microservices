<#
  Лаб. 09 — автоматична перевірка:
    1) матриця дозволів (таблиці 3.3.1–3.3.3): кожен endpoint × ролі admin / manager / user / guest (токени Keycloak);
    2) контрольні точки: manager з RPT (authorization.permissions) — PUT /orders/{id} = 200, DELETE = 403;
    3) декодовані JWT: Auth Service (roles), IdentityServer Client Credentials (без sub), Keycloak manager (permissions).
  Результати: docs/lab09/matrix-results.md і docs/lab09/jwt-*.json.

  Умови: працює Platform.AppHost, виконано pulumi up (infra/keycloak), користувачі змінили тимчасові паролі.
  Запуск з кореня репозиторію:
    powershell -ExecutionPolicy Bypass -File docs\lab09\check-lab09.ps1
#>
param(
    [string]$Gateway = "http://localhost:5000",
    [string]$Keycloak = "https://localhost:8080",
    [string]$IdentityServer = "https://localhost:5101",
    [string]$PostmanSecret = "postman-kc-secret",
    [string]$AdminPassword = "Alice#2026Pass",
    [string]$ManagerPassword = "Bob#2026Pass",
    [string]$UserPassword = "Carol#2026Pass",
    [string]$GuestPassword = "Dave#2026Pass"
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$OutDir = $PSScriptRoot
$TokenUrl = "$Keycloak/realms/DevRealmDrivers/protocol/openid-connect/token"

# ---------- допоміжні функції ----------
function Get-KeycloakToken([string]$User, [string]$Password) {
    (Invoke-RestMethod -Method Post -Uri $TokenUrl -Body @{
        grant_type = "password"; client_id = "postman"; client_secret = $PostmanSecret
        username = $User; password = $Password; scope = "openid"
    }).access_token
}

function Invoke-Api([string]$Method, [string]$Path, [string]$Token, $Body = $null) {
    $request = @{ Method = $Method; Uri = "$Gateway$Path"; Headers = @{ Authorization = "Bearer $Token" }; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $request.ContentType = "application/json; charset=utf-8"
        $request.Body = [Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 5 -Compress))
    }
    try {
        $response = Invoke-WebRequest @request
        return [pscustomobject]@{ Code = [int]$response.StatusCode; Content = $response.Content }
    }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        return [pscustomobject]@{ Code = [int]$_.Exception.Response.StatusCode; Content = "" }
    }
}

function ConvertFrom-Jwt([string]$Jwt) {
    $payload = $Jwt.Split('.')[1].Replace('-', '+').Replace('_', '/')
    switch ($payload.Length % 4) { 2 { $payload += '==' } 3 { $payload += '=' } }
    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json
}

function Save-Jwt([string]$Name, [string]$Jwt) {
    $claims = ConvertFrom-Jwt $Jwt
    Set-Content -Path (Join-Path $OutDir $Name) -Value ($claims | ConvertTo-Json -Depth 10) -Encoding UTF8
    return $claims
}

function New-OrderBody {
    @{ customerId = 1; shippingAddress = "Holovna St 1"; city = "Chernivtsi"; comment = "Lab 09 matrix check"
       items = @(@{ productId = 1; units = 1 }) }
}

# Окремий об'єкт для кожної перевірки, яка його змінює або видаляє (створює admin)
function New-Order {
    $created = Invoke-Api POST "/api/orders" $script:AdminToken (New-OrderBody)
    if ($created.Code -ne 201) { throw "Cannot create a test order: HTTP $($created.Code)" }
    ($created.Content | ConvertFrom-Json).id
}

function New-ProductBody {
    $suffix = Get-Random -Minimum 100000 -Maximum 999999
    @{ name = "Lab09 Test Serum $suffix"; sku = "L09-$suffix"; brandId = 1; price = 199; initialStock = 5; categoryIds = @(1) }
}

function New-Product {
    $created = Invoke-Api POST "/api/products" $script:AdminToken (New-ProductBody)
    if ($created.Code -eq 201) { return ($created.Content | ConvertFrom-Json).id }
    return 999999   # якщо тестовий товар не створився: дозволений запит дасть 404, заборонений — 403
}

function New-Payment {
    $created = Invoke-Api POST "/api/payments/process" $script:AdminToken @{ orderId = 1; amount = 100; currency = "UAH" }
    if ($created.Code -ne 201) { throw "Cannot create a test payment: HTTP $($created.Code)" }
    ($created.Content | ConvertFrom-Json).id
}

# ---------- токени Keycloak ----------
Write-Host "Keycloak tokens for alice / bob / carol / dave ..." -ForegroundColor Cyan
$script:AdminToken = Get-KeycloakToken "alice" $AdminPassword
$Tokens = [ordered]@{
    admin   = $script:AdminToken
    manager = Get-KeycloakToken "bob" $ManagerPassword
    user    = Get-KeycloakToken "carol" $UserPassword
    guest   = Get-KeycloakToken "dave" $GuestPassword
}

$productOneName = ((Invoke-Api GET "/api/products/1" $script:AdminToken).Content | ConvertFrom-Json).name

# ---------- матриця (таблиці 3.3.1–3.3.3) ----------
$all = @("admin", "manager", "user", "guest")
$rows = @(
    @{ Svc = "Orders";   Endpoint = "GET /api/orders";               Perm = "orders:read";      Allowed = @("admin", "manager", "user")
       Run = { param($t) Invoke-Api GET "/api/orders?customerId=1" $t } },
    @{ Svc = "Orders";   Endpoint = "POST /api/orders";              Perm = "orders:create";    Allowed = @("admin", "manager", "user")
       Run = { param($t) Invoke-Api POST "/api/orders" $t (New-OrderBody) } },
    @{ Svc = "Orders";   Endpoint = "PUT /api/orders/{id}";          Perm = "orders:update";    Allowed = @("admin", "manager")
       Run = { param($t) $id = New-Order; Invoke-Api PUT "/api/orders/$id" $t @{ shippingAddress = "Kobylianska St 14"; city = "Chernivtsi"; comment = "updated" } } },
    @{ Svc = "Orders";   Endpoint = "DELETE /api/orders/{id}";       Perm = "orders:delete";    Allowed = @("admin")
       Run = { param($t) $id = New-Order; Invoke-Api DELETE "/api/orders/$id" $t } },
    @{ Svc = "Orders";   Endpoint = "POST /api/orders/{id}/approve"; Perm = "orders:approve";   Allowed = @("admin", "manager")
       Run = { param($t) $id = New-Order; Invoke-Api POST "/api/orders/$id/approve" $t @{} } },
    @{ Svc = "Products"; Endpoint = "GET /api/products/{id}";        Perm = "products:read";    Allowed = $all
       Run = { param($t) Invoke-Api GET "/api/products/1" $t } },
    @{ Svc = "Products"; Endpoint = "POST /api/products";            Perm = "products:create";  Allowed = @("admin", "manager")
       Run = { param($t) Invoke-Api POST "/api/products" $t (New-ProductBody) } },
    @{ Svc = "Products"; Endpoint = "PUT /api/products/{id}/name";   Perm = "products:update";  Allowed = @("admin", "manager")
       Run = { param($t) Invoke-Api PUT "/api/products/1/name" $t @{ name = $productOneName } } },
    @{ Svc = "Products"; Endpoint = "DELETE /api/products/{id}";     Perm = "products:delete";  Allowed = @("admin")
       Run = { param($t) $id = New-Product; Invoke-Api DELETE "/api/products/$id" $t } },
    @{ Svc = "Products"; Endpoint = "POST /api/products/{id}/publish"; Perm = "products:publish"; Allowed = @("admin", "manager")
       Run = { param($t) Invoke-Api POST "/api/products/1/publish" $t @{} } },
    @{ Svc = "Payments"; Endpoint = "GET /api/payments";             Perm = "payments:read";    Allowed = @("admin")
       Run = { param($t) Invoke-Api GET "/api/payments" $t } },
    @{ Svc = "Payments"; Endpoint = "POST /api/payments/process";    Perm = "payments:process"; Allowed = @("admin")
       Run = { param($t) Invoke-Api POST "/api/payments/process" $t @{ orderId = 1; amount = 100; currency = "UAH" } } },
    @{ Svc = "Payments"; Endpoint = "POST /api/payments/refund";     Perm = "payments:refund";  Allowed = @("admin")
       Run = { param($t) $id = New-Payment; Invoke-Api POST "/api/payments/refund" $t @{ paymentId = $id; amount = 10; reason = "matrix check" } } }
)

$lines = New-Object System.Collections.Generic.List[string]
$matched = 0; $total = 0

foreach ($row in $rows) {
    $cells = @()
    foreach ($role in $Tokens.Keys) {
        $code = (& $row.Run $Tokens[$role]).Code
        $granted = ($code -ne 401) -and ($code -ne 403) -and ($code -lt 500)
        $expected = $row.Allowed -contains $role
        $ok = ($granted -eq $expected)
        $total++; if ($ok) { $matched++ }
        $mark = if ($ok) { [char]0x2705 } else { [char]0x274C }
        $cells += "$code $mark"
        $color = if ($ok) { "Green" } else { "Red" }
        Write-Host ("{0,-34} {1,-8} -> {2}" -f $row.Endpoint, $role, $code) -ForegroundColor $color
    }
    $allowedText = ($row.Allowed -join ", ")
    $lines.Add("| $($row.Svc) | ``$($row.Endpoint)`` | ``$($row.Perm)`` | $allowedText | $($cells -join ' | ') |")
}

# ---------- контрольні точки з RPT (authorization.permissions) ----------
Write-Host "RPT for bob (manager), audience orders-api ..." -ForegroundColor Cyan
$rpt = (Invoke-RestMethod -Method Post -Uri $TokenUrl -Headers @{ Authorization = "Bearer $($Tokens.manager)" } -Body @{
    grant_type = "urn:ietf:params:oauth:grant-type:uma-ticket"; audience = "orders-api"
}).access_token
$rptPut = (Invoke-Api PUT "/api/orders/$(New-Order)" $rpt @{ shippingAddress = "Kobylianska St 14"; city = "Chernivtsi"; comment = "manager RPT" }).Code
$rptDelete = (Invoke-Api DELETE "/api/orders/$(New-Order)" $rpt).Code
$userPayments = (Invoke-Api GET "/api/payments" $Tokens.user).Code

# ---------- декодовані JWT ----------
Write-Host "Decoding JWTs ..." -ForegroundColor Cyan
$authLogin = Invoke-RestMethod -Method Post -Uri "$Gateway/auth/login" -ContentType "application/json" `
    -Body '{"email":"admin@cosmetics.local","password":"Admin#2026Pass"}'
$authClaims = Save-Jwt "jwt-auth-service-admin.json" $authLogin.accessToken

$ccToken = (Invoke-RestMethod -Method Post -Uri "$IdentityServer/connect/token" -Body @{
    grant_type = "client_credentials"; client_id = "aggregator_service"; client_secret = "aggregator-secret"
    scope = "orders.read products.read reviews.read"
}).access_token
$ccClaims = Save-Jwt "jwt-identityserver-client-credentials.json" $ccToken

$managerClaims = Save-Jwt "jwt-keycloak-manager.json" $Tokens.manager
$rptClaims = Save-Jwt "jwt-keycloak-manager-rpt.json" $rpt

$ccHasSub = $ccClaims.PSObject.Properties.Name -contains "sub"
$rptPermissions = ($rptClaims.authorization.permissions | ForEach-Object { "$($_.rsname): $($_.scopes -join ', ')" }) -join "; "
$managerPermissions = @($managerClaims.permissions) -join ", "

# ---------- звіт ----------
$md = @"
# Лаб. 09 — перевірка матриці дозволів

Згенеровано скриптом ``docs/lab09/check-lab09.ps1`` $(Get-Date -Format "yyyy-MM-dd HH:mm"). Запити йдуть через Gateway ($Gateway) з токенами Keycloak (realm ``DevRealmDrivers``, клієнт ``postman``): alice — admin, bob — manager, carol — user, dave — guest.

Позначки: $([char]0x2705) — фактичний результат збігається з матрицею, $([char]0x274C) — не збігається. 401/403 — доступ заборонено; 200/201/204 — дозволено; 400/404/409 — доступ дозволено, але сервіс відхилив дані (авторизація пройдена до валідації).

**Збігів: $matched з $total.**

| Сервіс | Endpoint | Permission | Дозволено ролям | admin | manager | user | guest |
|---|---|---|---|---|---|---|---|
$($lines -join "`r`n")

## Контрольні точки (п. 3.6)

| Перевірка | Очікується | Фактично |
|---|---|---|
| manager (RPT з ``authorization.permissions``) ``PUT /api/orders/{id}`` | 200 | $rptPut |
| manager (RPT) ``DELETE /api/orders/{id}`` | 403 (а не 500) | $rptDelete |
| user ``GET /api/payments`` | 403 | $userPayments |

## Декодовані JWT

| Файл | Видавець | Що показує |
|---|---|---|
| [jwt-auth-service-admin.json](jwt-auth-service-admin.json) | Auth Service (ASP.NET Identity) | ``role``: $(@($authClaims.role) -join ', '); ``sub``: $($authClaims.sub) |
| [jwt-identityserver-client-credentials.json](jwt-identityserver-client-credentials.json) | Duende IdentityServer, Client Credentials | ``client_id``: $($ccClaims.client_id); ``scope``: $(@($ccClaims.scope) -join ' '); claim ``sub`` присутній: $ccHasSub |
| [jwt-keycloak-manager.json](jwt-keycloak-manager.json) | Keycloak, bob (manager), access token | ``realm_access.roles``: $(@($managerClaims.realm_access.roles) -join ', '); ``permissions``: $managerPermissions |
| [jwt-keycloak-manager-rpt.json](jwt-keycloak-manager-rpt.json) | Keycloak, bob (manager), RPT (``uma-ticket``, audience orders-api) | ``authorization.permissions``: $rptPermissions |
"@

Set-Content -Path (Join-Path $OutDir "matrix-results.md") -Value $md -Encoding UTF8

Write-Host ""
Write-Host "Matrix: $matched / $total match" -ForegroundColor $(if ($matched -eq $total) { "Green" } else { "Yellow" })
Write-Host "RPT manager: PUT -> $rptPut, DELETE -> $rptDelete; user GET payments -> $userPayments"
Write-Host "Client Credentials token has sub: $ccHasSub"
Write-Host "Saved: docs\lab09\matrix-results.md and docs\lab09\jwt-*.json" -ForegroundColor Cyan
