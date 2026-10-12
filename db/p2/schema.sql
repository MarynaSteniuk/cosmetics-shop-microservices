IF DB_ID(N'CosmeticsCatalogDb') IS NULL
    CREATE DATABASE CosmeticsCatalogDb;
GO

USE CosmeticsCatalogDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.Brands', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Brands (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Brands PRIMARY KEY,
        Name      NVARCHAR(100) NOT NULL CONSTRAINT UQ_Brands_Name UNIQUE,
        Country   NVARCHAR(60) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Brands_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name      NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
        Slug      NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Slug UNIQUE
                  CONSTRAINT CK_Categories_Slug CHECK (Slug NOT LIKE '% %'),
        SortOrder INT NOT NULL CONSTRAINT DF_Categories_SortOrder DEFAULT 0
                  CONSTRAINT CK_Categories_SortOrder CHECK (SortOrder >= 0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        BrandId   INT NOT NULL CONSTRAINT FK_Products_Brands REFERENCES dbo.Brands(Id),
        Sku       NVARCHAR(30) NOT NULL CONSTRAINT UQ_Products_Sku UNIQUE,
        Name      NVARCHAR(200) NOT NULL,
        Price     DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
        IsActive  BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );

    -- запит: пошук товару за назвою (WHERE Name LIKE N'текст%')
    CREATE INDEX IX_Products_Name
        ON dbo.Products (Name);

    -- запит: активні товари бренду, відсортовані за ціною
    CREATE INDEX IX_Products_BrandId_Price
        ON dbo.Products (BrandId, Price)
        INCLUDE (Name)
        WHERE IsActive = 1;
END
GO

IF OBJECT_ID(N'dbo.ProductDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductDetails (
        ProductId   BIGINT NOT NULL CONSTRAINT PK_ProductDetails PRIMARY KEY
                    CONSTRAINT FK_ProductDetails_Products REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        Description NVARCHAR(2000) NULL,
        Ingredients NVARCHAR(MAX) NULL,
        HowToUse    NVARCHAR(1000) NULL,
        Volume      DECIMAL(8,2) NOT NULL CONSTRAINT CK_ProductDetails_Volume CHECK (Volume > 0),
        VolumeUnit  NVARCHAR(5) NOT NULL CONSTRAINT DF_ProductDetails_VolumeUnit DEFAULT 'ml'
                    CONSTRAINT CK_ProductDetails_VolumeUnit CHECK (VolumeUnit IN ('ml','g')),
        SkinType    NVARCHAR(20) NOT NULL CONSTRAINT DF_ProductDetails_SkinType DEFAULT 'All'
                    CONSTRAINT CK_ProductDetails_SkinType CHECK (SkinType IN
                        ('All','Normal','Dry','Oily','Combination','Sensitive'))
    );

    -- запит: фільтр каталогу за типом шкіри
    CREATE INDEX IX_ProductDetails_SkinType
        ON dbo.ProductDetails (SkinType);
END
GO

IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategories (
        ProductId  BIGINT NOT NULL
                   CONSTRAINT FK_ProductCategories_Products REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        CategoryId INT NOT NULL
                   CONSTRAINT FK_ProductCategories_Categories REFERENCES dbo.Categories(Id) ON DELETE CASCADE,
        AddedAt    DATETIME2 NOT NULL CONSTRAINT DF_ProductCategories_AddedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProductCategories PRIMARY KEY (ProductId, CategoryId)
    );

    -- запит: товари категорії, спочатку нещодавно додані
    CREATE INDEX IX_ProductCategories_CategoryId_AddedAt
        ON dbo.ProductCategories (CategoryId, AddedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductImages (
        Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
        ProductId BIGINT NOT NULL
                  CONSTRAINT FK_ProductImages_Products REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        Url       NVARCHAR(500) NOT NULL,
        AltText   NVARCHAR(200) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_ProductImages_SortOrder DEFAULT 0
                  CONSTRAINT CK_ProductImages_SortOrder CHECK (SortOrder >= 0),
        IsMain    BIT NOT NULL CONSTRAINT DF_ProductImages_IsMain DEFAULT 0,
        CONSTRAINT UQ_ProductImages_ProductId_Url UNIQUE (ProductId, Url)
    );

    -- запит: галерея товару в заданому порядку
    CREATE INDEX IX_ProductImages_ProductId_SortOrder
        ON dbo.ProductImages (ProductId, SortOrder);

    -- обмеження: у товару може бути лише одне головне фото
    CREATE UNIQUE INDEX UX_ProductImages_MainPerProduct
        ON dbo.ProductImages (ProductId)
        WHERE IsMain = 1;
END
GO
