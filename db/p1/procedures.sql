USE CosmeticsOrdersDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateOrder
    @CustomerId      BIGINT,
    @ShippingAddress NVARCHAR(400),
    @City            NVARCHAR(100),
    @Comment         NVARCHAR(1000) = NULL,
    @PromoCode       NVARCHAR(30)   = NULL,
    @CreatedBy       NVARCHAR(100)  = NULL,
    @NewOrderId      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0)
        THROW 50020, 'Customer not found', 1;

    DECLARE @PromoCodeId BIGINT = NULL;

    IF @PromoCode IS NOT NULL
    BEGIN
        SELECT @PromoCodeId = Id
        FROM dbo.PromoCodes
        WHERE Code = @PromoCode
          AND IsActive = 1
          AND (ValidTo IS NULL OR ValidTo >= SYSUTCDATETIME());

        IF @PromoCodeId IS NULL
            THROW 50021, 'Promo code is invalid or expired', 1;
    END

    BEGIN TRANSACTION;

    INSERT dbo.Orders (CustomerId, PromoCodeId, CreatedBy)
    VALUES (@CustomerId, @PromoCodeId, @CreatedBy);

    SET @NewOrderId = SCOPE_IDENTITY();

    INSERT dbo.OrderDetails (OrderId, ShippingAddress, City, Comment)
    VALUES (@NewOrderId, @ShippingAddress, @City, @Comment);

    INSERT dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy)
    VALUES (@NewOrderId, NULL, 'Submitted', @CreatedBy);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AddOrderItem
    @OrderId   BIGINT,
    @ProductId BIGINT,
    @Units     INT,
    @UpdatedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Units IS NULL OR @Units <= 0
        THROW 50030, 'Units must be positive', 1;

    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL
        THROW 50001, 'Order not found', 1;

    IF @Status <> 'Submitted'
        THROW 50031, 'Items can be changed only while the order is Submitted', 1;

    DECLARE @Name NVARCHAR(200), @Price DECIMAL(18,2);

    SELECT @Name = Name, @Price = Price
    FROM dbo.Products
    WHERE Id = @ProductId AND IsDeleted = 0;

    IF @Name IS NULL
        THROW 50032, 'Product not found', 1;

    DECLARE @Percent DECIMAL(5,2) = ISNULL((
        SELECT pc.DiscountPercent
        FROM dbo.Orders AS o
        JOIN dbo.PromoCodes AS pc ON pc.Id = o.PromoCodeId
        WHERE o.Id = @OrderId), 0);

    DECLARE @Discount DECIMAL(18,2) = CAST(ROUND(@Price * @Percent / 100.0, 2) AS DECIMAL(18,2));

    IF EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId AND ProductId = @ProductId)
    BEGIN
        UPDATE dbo.OrderItems
        SET Units = Units + @Units
        WHERE OrderId = @OrderId AND ProductId = @ProductId;
    END
    ELSE
    BEGIN
        INSERT dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Discount, Units)
        VALUES (@OrderId, @ProductId, @Name, @Price, @Discount, @Units);
    END

    UPDATE dbo.Orders
    SET TotalAmount = (SELECT SUM((UnitPrice - Discount) * Units)
                       FROM dbo.OrderItems WHERE OrderId = @OrderId),
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @OrderId;

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetOrderById
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE Id = @OrderId AND IsDeleted = 0)
        THROW 50001, 'Order not found', 1;

    SELECT o.Id, o.CustomerId, c.FullName, c.Email,
           o.Status, o.OrderDate, o.TotalAmount,
           pc.Code AS PromoCode,
           d.ShippingAddress, d.City, d.Comment,
           o.RowVersion
    FROM dbo.Orders AS o
    JOIN dbo.Customers AS c ON c.Id = o.CustomerId
    JOIN dbo.OrderDetails AS d ON d.OrderId = o.Id
    LEFT JOIN dbo.PromoCodes AS pc ON pc.Id = o.PromoCodeId
    WHERE o.Id = @OrderId AND o.IsDeleted = 0;

    SELECT ProductId, ProductName, UnitPrice, Discount, Units,
           (UnitPrice - Discount) * Units AS LineTotal
    FROM dbo.OrderItems
    WHERE OrderId = @OrderId
    ORDER BY ProductName;

    SELECT Id, OldStatus, NewStatus, ChangedAt, ChangedBy
    FROM dbo.OrderStatusHistory
    WHERE OrderId = @OrderId
    ORDER BY ChangedAt, Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpdateOrderDetails
    @OrderId         BIGINT,
    @ShippingAddress NVARCHAR(400),
    @City            NVARCHAR(100),
    @Comment         NVARCHAR(1000) = NULL,
    @UpdatedBy       NVARCHAR(100)  = NULL,
    @RowVersion      BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL
        THROW 50001, 'Order not found', 1;

    IF @Status NOT IN ('Submitted', 'AwaitingValidation')
        THROW 50011, 'Details can be edited only before stock confirmation', 1;

    UPDATE dbo.Orders
    SET UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @OrderId AND RowVersion = @RowVersion;

    IF @@ROWCOUNT = 0
        THROW 50010, 'Order was modified by another user. Reload and try again', 1;

    UPDATE dbo.OrderDetails
    SET ShippingAddress = @ShippingAddress,
        City = @City,
        Comment = @Comment
    WHERE OrderId = @OrderId;

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ChangeOrderStatus
    @OrderId   BIGINT,
    @NewStatus NVARCHAR(30),
    @ChangedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @OldStatus NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @OldStatus IS NULL
        THROW 50001, 'Order not found', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM (VALUES
            ('Submitted',          'AwaitingValidation'),
            ('Submitted',          'Cancelled'),
            ('AwaitingValidation', 'StockConfirmed'),
            ('AwaitingValidation', 'Cancelled'),
            ('StockConfirmed',     'Paid'),
            ('StockConfirmed',     'Cancelled'),
            ('Paid',               'Shipped')
        ) AS T (FromStatus, ToStatus)
        WHERE T.FromStatus = @OldStatus AND T.ToStatus = @NewStatus)
    BEGIN
        DECLARE @Msg NVARCHAR(200) =
            CONCAT(N'Transition ', @OldStatus, N' -> ', @NewStatus, N' is not allowed');
        THROW 50002, @Msg, 1;
    END

    IF @NewStatus = 'AwaitingValidation'
       AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId)
        THROW 50003, 'Order has no items', 1;

    IF @NewStatus = 'StockConfirmed'
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM dbo.OrderItems AS oi
            JOIN dbo.Products AS p WITH (UPDLOCK) ON p.Id = oi.ProductId
            WHERE oi.OrderId = @OrderId AND p.Stock < oi.Units)
            THROW 50004, 'Insufficient stock for one or more items', 1;

        UPDATE p
        SET p.Stock = p.Stock - oi.Units,
            p.UpdatedAt = SYSUTCDATETIME(),
            p.UpdatedBy = @ChangedBy
        FROM dbo.Products AS p
        JOIN dbo.OrderItems AS oi ON oi.ProductId = p.Id
        WHERE oi.OrderId = @OrderId;
    END

    IF @NewStatus = 'Cancelled' AND @OldStatus = 'StockConfirmed'
    BEGIN
        UPDATE p
        SET p.Stock = p.Stock + oi.Units,
            p.UpdatedAt = SYSUTCDATETIME(),
            p.UpdatedBy = @ChangedBy
        FROM dbo.Products AS p
        JOIN dbo.OrderItems AS oi ON oi.ProductId = p.Id
        WHERE oi.OrderId = @OrderId;
    END

    UPDATE dbo.Orders
    SET Status = @NewStatus,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @ChangedBy
    WHERE Id = @OrderId;

    INSERT dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy)
    VALUES (@OrderId, @OldStatus, @NewStatus, @ChangedBy);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteOrder
    @OrderId   BIGINT,
    @DeletedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL
        THROW 50001, 'Order not found', 1;

    IF @Status NOT IN ('Submitted', 'Cancelled')
        THROW 50040, 'Only Submitted or Cancelled orders can be deleted', 1;

    UPDATE dbo.Orders
    SET IsDeleted = 1,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @DeletedBy
    WHERE Id = @OrderId;

    COMMIT TRANSACTION;
END
GO
