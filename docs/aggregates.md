# Агрегати і межі даних

## CosmeticsOrdersDb

Order — головний агрегат. До нього входять Orders, OrderDetails, OrderItems і OrderStatusHistory. Адреса, позиції та історія статусів без замовлення не мають сенсу, тому створюються разом із ним через usp_CreateOrder і usp_AddOrderItem, а при фізичному видаленні замовлення видаляються каскадно. Звичайне видалення м'яке: IsDeleted = 1.
Статус рухається так: Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped. Скасувати (Cancelled) можна до оплати. Якщо скасовують після StockConfirmed, товар повертається на склад.

Customer — окремий агрегат. Один покупець має багато замовлень. Видалити покупця з історією замовлень не можна (FK без каскаду), його лише позначають IsDeleted.

PromoCode — окремий довідник. Замовлення може мати промокод, а може не мати (PromoCodeId допускає NULL).

## CosmeticsCatalogDb

Product — головний агрегат: Products, ProductDetails, ProductImages, ProductCategories. Склад, фото та прив'язки до категорій належать конкретному товару і видаляються разом із ним. Головне фото в товару може бути тільки одне.

Brand — довідник брендів. Бренд, у якого є товари, видалити не можна.

Category — довідник категорій. Якщо категорію видалити, зникнуть лише записи в ProductCategories, самі товари лишаться.

## cosmetics_reviews

Review — документ відгуку разом з об'єктом author і масивом comments. Коментарі короткі й нечисленні, тому живуть у самому відгуку.

Discussion — документ теми. Повідомлення лежать у discussion_messages і посилаються на тему через discussionId. У темі зберігаються лічильник messagesCount і масив recentMessages з останніми повідомленнями, щоб список тем показувався одним запитом. Відгук може містити discussionId, якщо до нього відкрито тему.

ProductRating — документ product_ratings. Це не окремі дані, а підсумок по reviews: seed.js перераховує його через aggregate з $merge.

## Що дублюється між базами

| Поле | Де зберігається | Джерело (власник даних) | Політика узгодженості |
|---|---|---|---|
| OrderItems.ProductId | CosmeticsOrdersDb | Products у CosmeticsCatalogDb | звичайний стовпець з Id товару, FOREIGN KEY на іншу базу немає |
| OrderItems.ProductName, UnitPrice | CosmeticsOrdersDb | Products у CosmeticsCatalogDb | знімок: записується, коли товар додають у замовлення, і далі не змінюється |
| Products.Name, Price (локальний довідник) | CosmeticsOrdersDb | Products у CosmeticsCatalogDb | оновлюється, коли Каталог надсилає подію про зміну товару |
| reviews.productId, discussions.productId | cosmetics_reviews | Products у CosmeticsCatalogDb | число з Id товару, без зовнішнього ключа |
| reviews.productName, discussions.productName | cosmetics_reviews | Products у CosmeticsCatalogDb | оновлюється за подією про зміну товару з Каталогу |
| reviews.author.customerId | cosmetics_reviews | Customers у CosmeticsOrdersDb | Id покупця, без зовнішнього ключа |
| reviews.author.displayName, discussion_messages.author.displayName | cosmetics_reviews | Customers у CosmeticsOrdersDb | оновлюється за подією про зміну профілю покупця |
