IF DB_ID(N'CosmeticsOrdersDb') IS NULL
    CREATE DATABASE CosmeticsOrdersDb;
GO

USE CosmeticsOrdersDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers (
        Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        Email      NVARCHAR(256) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
        FullName   NVARCHAR(200) NOT NULL,
        Phone      NVARCHAR(20)  NULL,
        CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy  NVARCHAR(100) NULL,
        UpdatedAt  DATETIME2 NULL,
        UpdatedBy  NVARCHAR(100) NULL,
        IsDeleted  BIT NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
        RowVersion ROWVERSION
    );
END
GO

IF OBJECT_ID(N'dbo.PromoCodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PromoCodes (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromoCodes PRIMARY KEY,
        Code            NVARCHAR(30) NOT NULL CONSTRAINT UQ_PromoCodes_Code UNIQUE,
        DiscountPercent DECIMAL(5,2) NOT NULL
                        CONSTRAINT CK_PromoCodes_Percent CHECK (DiscountPercent BETWEEN 1 AND 90),
        ValidTo         DATETIME2 NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_PromoCodes_IsActive DEFAULT 1,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_PromoCodes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy       NVARCHAR(100) NULL
    );
END
GO

-- Products: локальна копія товарів Каталогу. Id той самий, що в БД Каталогу, FK на іншу БД немає.
-- Name і Price оновлюються подіями з Каталогу.
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id         BIGINT NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Name       NVARCHAR(200) NOT NULL,
        Price      DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
        Stock      INT NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0
                   CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
        CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy  NVARCHAR(100) NULL,
        UpdatedAt  DATETIME2 NULL,
        UpdatedBy  NVARCHAR(100) NULL,
        IsDeleted  BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT 0,
        RowVersion ROWVERSION
    );
END
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        CustomerId  BIGINT NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(Id),
        PromoCodeId BIGINT NULL CONSTRAINT FK_Orders_PromoCodes REFERENCES dbo.PromoCodes(Id),
        Status      NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 'Submitted'
                    CONSTRAINT CK_Orders_Status CHECK (Status IN
                        ('Submitted','AwaitingValidation','StockConfirmed','Paid','Shipped','Cancelled')),
        OrderDate   DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSUTCDATETIME(),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT 0
                    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0),
        CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy   NVARCHAR(100) NULL,
        UpdatedAt   DATETIME2 NULL,
        UpdatedBy   NVARCHAR(100) NULL,
        IsDeleted   BIT NOT NULL CONSTRAINT DF_Orders_IsDeleted DEFAULT 0,
        RowVersion  ROWVERSION
    );

    -- запит: замовлення клієнта з певним статусом (особистий кабінет)
    CREATE INDEX IX_Orders_CustomerId_Status
        ON dbo.Orders (CustomerId, Status)
        INCLUDE (OrderDate, TotalAmount)
        WHERE IsDeleted = 0;

    -- запит: замовлення в певному статусі за датою (наприклад, оплачені, що чекають відправки)
    CREATE INDEX IX_Orders_Status_OrderDate
        ON dbo.Orders (Status, OrderDate)
        WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderDetails (
        OrderId         BIGINT NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY
                        CONSTRAINT FK_OrderDetails_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        ShippingAddress NVARCHAR(400) NOT NULL,
        City            NVARCHAR(100) NOT NULL,
        Comment         NVARCHAR(1000) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems (
        OrderId     BIGINT NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        ProductId   BIGINT NOT NULL CONSTRAINT FK_OrderItems_Products REFERENCES dbo.Products(Id),
        ProductName NVARCHAR(200) NOT NULL,   -- знімок на момент замовлення, не оновлюється
        UnitPrice   DECIMAL(18,2) NOT NULL    -- знімок на момент замовлення, не оновлюється
                    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0),
        Discount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_Discount DEFAULT 0
                    CONSTRAINT CK_OrderItems_Discount CHECK (Discount >= 0),
        Units       INT NOT NULL CONSTRAINT CK_OrderItems_Units CHECK (Units > 0),
        CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, ProductId),
        CONSTRAINT CK_OrderItems_DiscountLtPrice CHECK (Discount < UnitPrice)
    );

    -- запит: у яких замовленнях є товар і скільки одиниць продано
    CREATE INDEX IX_OrderItems_ProductId
        ON dbo.OrderItems (ProductId)
        INCLUDE (Units);
END
GO

IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderStatusHistory (
        Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderStatusHistory PRIMARY KEY,
        OrderId   BIGINT NOT NULL
                  CONSTRAINT FK_OrderStatusHistory_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        OldStatus NVARCHAR(30) NULL,
        NewStatus NVARCHAR(30) NOT NULL
                  CONSTRAINT CK_OrderStatusHistory_NewStatus CHECK (NewStatus IN
                      ('Submitted','AwaitingValidation','StockConfirmed','Paid','Shipped','Cancelled')),
        ChangedAt DATETIME2 NOT NULL CONSTRAINT DF_OrderStatusHistory_ChangedAt DEFAULT SYSUTCDATETIME(),
        ChangedBy NVARCHAR(100) NULL
    );

    -- запит: історія статусів замовлення в хронологічному порядку
    CREATE INDEX IX_OrderStatusHistory_OrderId_ChangedAt
        ON dbo.OrderStatusHistory (OrderId, ChangedAt);
END
GO
