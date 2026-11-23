# Лаб. 09 — перевірка матриці дозволів

Згенеровано скриптом `docs/lab09/check-lab09.ps1` 2026-11-23 21:56. Запити йдуть через Gateway (http://localhost:5000) з токенами Keycloak (realm `DevRealmDrivers`, клієнт `postman`): alice — admin, bob — manager, carol — user, dave — guest.

Позначки: ✅ — фактичний результат збігається з матрицею, ❌ — не збігається. 401/403 — доступ заборонено; 200/201/204 — дозволено; 400/404/409 — доступ дозволено, але сервіс відхилив дані (авторизація пройдена до валідації).

**Збігів: 52 з 52.**

| Сервіс | Endpoint | Permission | Дозволено ролям | admin | manager | user | guest |
|---|---|---|---|---|---|---|---|
| Orders | `GET /api/orders` | `orders:read` | admin, manager, user | 200 ✅ | 200 ✅ | 200 ✅ | 403 ✅ |
| Orders | `POST /api/orders` | `orders:create` | admin, manager, user | 201 ✅ | 201 ✅ | 201 ✅ | 403 ✅ |
| Orders | `PUT /api/orders/{id}` | `orders:update` | admin, manager | 200 ✅ | 200 ✅ | 403 ✅ | 403 ✅ |
| Orders | `DELETE /api/orders/{id}` | `orders:delete` | admin | 204 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |
| Orders | `POST /api/orders/{id}/approve` | `orders:approve` | admin, manager | 204 ✅ | 204 ✅ | 403 ✅ | 403 ✅ |
| Products | `GET /api/products/{id}` | `products:read` | admin, manager, user, guest | 200 ✅ | 200 ✅ | 200 ✅ | 200 ✅ |
| Products | `POST /api/products` | `products:create` | admin, manager | 201 ✅ | 201 ✅ | 403 ✅ | 403 ✅ |
| Products | `PUT /api/products/{id}/name` | `products:update` | admin, manager | 200 ✅ | 200 ✅ | 403 ✅ | 403 ✅ |
| Products | `DELETE /api/products/{id}` | `products:delete` | admin | 204 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |
| Products | `POST /api/products/{id}/publish` | `products:publish` | admin, manager | 200 ✅ | 200 ✅ | 403 ✅ | 403 ✅ |
| Payments | `GET /api/payments` | `payments:read` | admin | 200 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |
| Payments | `POST /api/payments/process` | `payments:process` | admin | 201 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |
| Payments | `POST /api/payments/refund` | `payments:refund` | admin | 200 ✅ | 403 ✅ | 403 ✅ | 403 ✅ |

## Контрольні точки (п. 3.6)

| Перевірка | Очікується | Фактично |
|---|---|---|
| manager (RPT з `authorization.permissions`) `PUT /api/orders/{id}` | 200 | 200 |
| manager (RPT) `DELETE /api/orders/{id}` | 403 (а не 500) | 403 |
| user `GET /api/payments` | 403 | 403 |

## Декодовані JWT

| Файл | Видавець | Що показує |
|---|---|---|
| [jwt-auth-service-admin.json](jwt-auth-service-admin.json) | Auth Service (ASP.NET Identity) | `role`: User, Admin; `sub`: 256cacd9-ed83-47c3-b844-55d7cde64376 |
| [jwt-identityserver-client-credentials.json](jwt-identityserver-client-credentials.json) | Duende IdentityServer, Client Credentials | `client_id`: aggregator_service; `scope`: orders.read products.read reviews.read; claim `sub` присутній: False |
| [jwt-keycloak-manager.json](jwt-keycloak-manager.json) | Keycloak, bob (manager), access token | `realm_access.roles`: manager, offline_access, uma_authorization, default-roles-devrealmdrivers; `permissions`: products:read, products:update, products:create, products:publish, orders:read, orders:approve, orders:create, orders:update |
| [jwt-keycloak-manager-rpt.json](jwt-keycloak-manager-rpt.json) | Keycloak, bob (manager), RPT (`uma-ticket`, audience orders-api) | `authorization.permissions`: orders: orders:read, orders:create, orders:approve, orders:update |
