# Перевірка баз

Запити, які навмисно мають дати помилку, позначені «помилка». Їх зручно виконувати по одному: виділити і натиснути Execute.

## CosmeticsOrdersDb

```sql
USE CosmeticsOrdersDb;

-- зовнішні ключі є тільки між таблицями цієї бази
SELECT name, OBJECT_NAME(parent_object_id) AS child, OBJECT_NAME(referenced_object_id) AS parent
FROM sys.foreign_keys;

-- індекси таблиці Orders і їхні фільтри
SELECT name, type_desc, filter_definition FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Orders');

-- зведення замовлень
SELECT OrderId, CustomerName, Status, ItemsCount, TotalAmount FROM dbo.vw_OrderSummary;

-- замовлення покупця з певним статусом (працює індекс IX_Orders_CustomerId_Status)
SELECT Id, OrderDate, TotalAmount FROM dbo.Orders
WHERE CustomerId = 2 AND Status = 'Paid' AND IsDeleted = 0;

-- друга сторінка покупців по 2 записи
SELECT Id, FullName FROM dbo.Customers ORDER BY FullName OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY;

-- історія статусів оплаченого замовлення
EXEC dbo.usp_GetOrderById @OrderId = 2;
```

Повний цикл: створити, наповнити, змінити адресу, видалити.

```sql
DECLARE @id BIGINT;
EXEC dbo.usp_CreateOrder @CustomerId = 3, @ShippingAddress = N'вул. Кобилянської, 14', @City = N'Чернівці',
     @PromoCode = N'SPRING15', @CreatedBy = N'check', @NewOrderId = @id OUTPUT;
EXEC dbo.usp_AddOrderItem @OrderId = @id, @ProductId = 7, @Units = 2;
EXEC dbo.usp_AddOrderItem @OrderId = @id, @ProductId = 7, @Units = 1;   -- кількість стане 3

DECLARE @ver BINARY(8) = (SELECT RowVersion FROM dbo.Orders WHERE Id = @id);
EXEC dbo.usp_UpdateOrderDetails @OrderId = @id, @ShippingAddress = N'вул. Кобилянської, 16',
     @City = N'Чернівці', @UpdatedBy = N'check', @RowVersion = @ver;
EXEC dbo.usp_GetOrderById @OrderId = @id;
EXEC dbo.usp_SoftDeleteOrder @OrderId = @id, @DeletedBy = N'check';
```

Бізнес-правила і обмеження (помилка):

```sql
EXEC dbo.usp_ChangeOrderStatus @OrderId = 1, @NewStatus = 'Paid';       -- 50002: не можна оплатити, минаючи перевірку
EXEC dbo.usp_ChangeOrderStatus @OrderId = 3, @NewStatus = 'Submitted';  -- 50002: зі Cancelled виходу немає
EXEC dbo.usp_AddOrderItem @OrderId = 2, @ProductId = 1, @Units = 1;     -- 50031: оплачене замовлення не змінюється
UPDATE dbo.Products SET Stock = -1 WHERE Id = 8;                        -- CHECK: залишок не може бути від'ємним
INSERT dbo.Customers (Email, FullName) VALUES (N'andrii.melnyk@example.com', N'Дубль');  -- UNIQUE e-mail
```

## CosmeticsCatalogDb

```sql
USE CosmeticsCatalogDb;

SELECT name, OBJECT_NAME(parent_object_id) AS child, OBJECT_NAME(referenced_object_id) AS parent
FROM sys.foreign_keys;

-- вітрина
SELECT Name, Brand, Price, SkinType, Categories FROM dbo.vw_ProductCatalog;

-- засоби для жирної шкіри дешевші за 800 грн
SELECT p.Name, p.Price FROM dbo.Products p
JOIN dbo.ProductDetails d ON d.ProductId = p.Id
WHERE d.SkinType = 'Oily' AND p.Price < 800
ORDER BY p.Price;

-- пошук за початком назви (індекс IX_Products_Name)
SELECT Id, Name FROM dbo.Products WHERE Name LIKE N'La Roche%';

-- що нового в категорії «Зволоження»
SELECT p.Name, pc.AddedAt FROM dbo.ProductCategories pc
JOIN dbo.Products p ON p.Id = pc.ProductId
WHERE pc.CategoryId = 2
ORDER BY pc.AddedAt DESC;

-- третя сторінка товарів по 3
SELECT Id, Name FROM dbo.Products ORDER BY Id OFFSET 6 ROWS FETCH NEXT 3 ROWS ONLY;
```

Обмеження (помилка):

```sql
UPDATE dbo.Products SET Price = 0 WHERE Id = 3;                                   -- CHECK Price > 0
INSERT dbo.Categories (Name, Slug) VALUES (N'Маски', N'face masks');              -- CHECK: slug без пробілів
INSERT dbo.ProductImages (ProductId, Url, IsMain) VALUES (3, N'/images/products/3/new.jpg', 1);  -- друге головне фото
```

## Повторний seed

Після другого запуску обох seed.sql цифри мають бути ті самі:

```sql
SELECT (SELECT COUNT(*) FROM CosmeticsOrdersDb.dbo.Customers)  AS customers,
       (SELECT COUNT(*) FROM CosmeticsOrdersDb.dbo.PromoCodes) AS promo,
       (SELECT COUNT(*) FROM CosmeticsCatalogDb.dbo.Brands)     AS brands,
       (SELECT COUNT(*) FROM CosmeticsCatalogDb.dbo.ProductImages) AS images;
```

## cosmetics_reviews

```javascript
use cosmetics_reviews

// різні набори полів в одній колекції
db.reviews.find({}, { productName: 1, photos: 1, video: 1, usageDuration: 1, repurchase: 1 })

// відгуки покупців із чутливою шкірою
db.reviews.find({ "author.skinType": "Sensitive" }, { productName: 1, rating: 1, title: 1 })

// теми з тегом «чутлива шкіра», найактивніші зверху
db.discussions.find({ tags: "чутлива шкіра" }).sort({ lastMessageAt: -1 })

// кількість відгуків з оцінкою 5 по товарах
db.reviews.aggregate([
  { $match: { rating: 5 } },
  { $group: { _id: "$productId", fiveStars: { $sum: 1 } } },
  { $sort: { fiveStars: -1 } }
])

// лайк відгуку
db.reviews.updateOne({ _id: ObjectId("670a0000000000000000f005") }, { $inc: { helpfulVotes: 1 } })

// помилка валідації: rating має бути від 1 до 5
db.reviews.insertOne({ productId: 4, productName: "Bioderma Sensibio H2O 500 ml",
  author: { displayName: "Тест" }, rating: 0, createdAt: new Date() })
```
