USE CosmeticsOrdersDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'WELCOME10')
    INSERT dbo.PromoCodes (Code, DiscountPercent, ValidTo, IsActive, CreatedBy)
    VALUES (N'WELCOME10', 10, NULL, 1, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'SPRING15')
    INSERT dbo.PromoCodes (Code, DiscountPercent, ValidTo, IsActive, CreatedBy)
    VALUES (N'SPRING15', 15, '2027-12-31', 1, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.PromoCodes WHERE Code = N'OLD5')
    INSERT dbo.PromoCodes (Code, DiscountPercent, ValidTo, IsActive, CreatedBy)
    VALUES (N'OLD5', 5, '2025-01-01', 0, N'seed');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'olena.kovalenko@example.com')
    INSERT dbo.Customers (Email, FullName, Phone, CreatedBy)
    VALUES (N'olena.kovalenko@example.com', N'Олена Коваленко', N'+380501112233', N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'andrii.melnyk@example.com')
    INSERT dbo.Customers (Email, FullName, Phone, CreatedBy)
    VALUES (N'andrii.melnyk@example.com', N'Андрій Мельник', N'+380672223344', N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'iryna.shevchenko@example.com')
    INSERT dbo.Customers (Email, FullName, Phone, CreatedBy)
    VALUES (N'iryna.shevchenko@example.com', N'Ірина Шевченко', NULL, N'seed');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 1)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (1, N'CeraVe Hydrating Cleanser 236 ml', 520.00, 50, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 2)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (2, N'La Roche-Posay Effaclar Duo+ 40 ml', 780.00, 30, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 3)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (3, N'The Ordinary Niacinamide 10% + Zinc 1% 30 ml', 450.00, 40, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 4)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (4, N'Bioderma Sensibio H2O 500 ml', 610.00, 25, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 5)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (5, N'Neutrogena Hydro Boost Water Gel 50 ml', 430.00, 35, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 6)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (6, N'Vichy Minéral 89 Serum 50 ml', 890.00, 20, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 7)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (7, N'Cetaphil Moisturizing Cream 250 g', 560.00, 30, N'seed');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 8)
    INSERT dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (8, N'Avène Cleanance SPF50+ 50 ml', 700.00, 15, N'seed');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE CreatedBy = N'seed')
BEGIN
    DECLARE @Olena BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'olena.kovalenko@example.com');
    DECLARE @Andrii BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'andrii.melnyk@example.com');
    DECLARE @Iryna BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'iryna.shevchenko@example.com');
    DECLARE @o1 BIGINT, @o2 BIGINT, @o3 BIGINT;

    EXEC dbo.usp_CreateOrder @CustomerId = @Olena,
         @ShippingAddress = N'вул. Головна, 10, кв. 5', @City = N'Чернівці',
         @Comment = N'Подзвонити перед доставкою', @PromoCode = N'WELCOME10',
         @CreatedBy = N'seed', @NewOrderId = @o1 OUTPUT;
    EXEC dbo.usp_AddOrderItem @OrderId = @o1, @ProductId = 1, @Units = 2, @UpdatedBy = N'seed';
    EXEC dbo.usp_AddOrderItem @OrderId = @o1, @ProductId = 2, @Units = 1, @UpdatedBy = N'seed';

    EXEC dbo.usp_CreateOrder @CustomerId = @Andrii,
         @ShippingAddress = N'вул. Незалежності, 25', @City = N'Львів',
         @Comment = NULL, @PromoCode = NULL,
         @CreatedBy = N'seed', @NewOrderId = @o2 OUTPUT;
    EXEC dbo.usp_AddOrderItem @OrderId = @o2, @ProductId = 3, @Units = 1, @UpdatedBy = N'seed';
    EXEC dbo.usp_AddOrderItem @OrderId = @o2, @ProductId = 5, @Units = 2, @UpdatedBy = N'seed';
    EXEC dbo.usp_ChangeOrderStatus @OrderId = @o2, @NewStatus = 'AwaitingValidation', @ChangedBy = N'seed';
    EXEC dbo.usp_ChangeOrderStatus @OrderId = @o2, @NewStatus = 'StockConfirmed',     @ChangedBy = N'seed';
    EXEC dbo.usp_ChangeOrderStatus @OrderId = @o2, @NewStatus = 'Paid',               @ChangedBy = N'seed';

    EXEC dbo.usp_CreateOrder @CustomerId = @Iryna,
         @ShippingAddress = N'просп. Шевченка, 3', @City = N'Київ',
         @Comment = N'Подарунок', @PromoCode = N'SPRING15',
         @CreatedBy = N'seed', @NewOrderId = @o3 OUTPUT;
    EXEC dbo.usp_AddOrderItem @OrderId = @o3, @ProductId = 6, @Units = 1, @UpdatedBy = N'seed';
    EXEC dbo.usp_ChangeOrderStatus @OrderId = @o3, @NewStatus = 'Cancelled', @ChangedBy = N'seed';
END
GO
