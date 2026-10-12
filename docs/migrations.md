# Зміни схем баз даних

## Каталог (CosmeticsCatalogDb) — міграції EF Core
Зараз схема каталогу задана скриптом db/p2/schema.sql. Далі ця база буде реалізована через EF Core Code First, і кожна зміна структури оформлюватиметься міграцією: метод Up застосовує зміну, Down відкочує її. Перша міграція відтворить поточну схему:
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Наприклад, щоб для сонцезахисних засобів зберігати рівень SPF, у клас Product додається властивість Spf, а потім:
```
dotnet ef migrations add AddProductSpf
dotnet ef database update
```

## Замовлення (CosmeticsOrdersDb) — SQL-скрипти
Тут працюють ADO.NET і Dapper без EF, тому схема змінюється скриптами з db/p1. schema.sql створює лише відсутні таблиці, процедури й представлення пишуться через CREATE OR ALTER, тож скрипти можна запускати повторно. Майбутні зміни додаватимуться окремими пронумерованими скриптами з ALTER TABLE.

| Версія | Що є в схемі |
|---|---|
| 1 | 7 таблиць (Customers, PromoCodes, Products, Orders, OrderDetails, OrderItems, OrderStatusHistory), зв'язки 1:1, 1:N, M:N, ключі та обмеження, 4 індекси під запити, аудит, IsDeleted, RowVersion, vw_OrderSummary, 6 процедур |

## Відгуки (cosmetics_reviews) — валідація і міграції документів
collections.js задає для reviews, discussions і discussion_messages правила $jsonSchema: які поля обов'язкові, що rating — ціле число від 1 до 5, що в recentMessages не більше трьох елементів. Відгук з оцінкою 7 база відхилить. Решта полів вільна, тому документи можуть відрізнятися. Якщо правила змінюються, collections.js запускається знову і оновлює їх через collMod.

Якщо в застосунку з'явиться модерація відгуків, старим відгукам треба буде проставити статус. Щоб такий скрипт не виконався двічі, номер зміни зберігається в колекції schema_versions:
```javascript
const version = "2026-10-moderation";
if (!db.schema_versions.findOne({ _id: version })) {
  db.reviews.updateMany({ moderation: { $exists: false } }, { $set: { moderation: "approved" } });
  db.schema_versions.insertOne({ _id: version, appliedAt: new Date() });
}
```
