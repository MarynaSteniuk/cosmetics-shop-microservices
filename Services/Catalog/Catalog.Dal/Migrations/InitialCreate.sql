IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE TABLE [Brands] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Country] nvarchar(60) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Categories_Slug] CHECK ([Slug] NOT LIKE '% %'),
        CONSTRAINT [CK_Categories_SortOrder] CHECK ([SortOrder] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] bigint NOT NULL IDENTITY,
        [BrandId] int NOT NULL,
        [Sku] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [AvailableStock] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Products_AvailableStock] CHECK ([AvailableStock] >= 0),
        CONSTRAINT [CK_Products_Price] CHECK ([Price] > 0),
        CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [ProductId] bigint NOT NULL,
        [CategoryId] int NOT NULL,
        [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([ProductId], [CategoryId]),
        CONSTRAINT [FK_ProductCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductCategories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductDetails] (
        [ProductId] bigint NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Ingredients] nvarchar(max) NULL,
        [HowToUse] nvarchar(1000) NULL,
        [Volume] decimal(8,2) NOT NULL,
        [VolumeUnit] nvarchar(5) NOT NULL,
        [SkinType] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_ProductDetails] PRIMARY KEY ([ProductId]),
        CONSTRAINT [CK_ProductDetails_SkinType] CHECK ([SkinType] IN ('All', 'Normal', 'Dry', 'Oily', 'Combination', 'Sensitive')),
        CONSTRAINT [CK_ProductDetails_Volume] CHECK ([Volume] > 0),
        CONSTRAINT [CK_ProductDetails_VolumeUnit] CHECK ([VolumeUnit] IN ('ml', 'g')),
        CONSTRAINT [FK_ProductDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [AltText] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [IsMain] bit NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductImages_SortOrder] CHECK ([SortOrder] >= 0),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'CreatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Brands]'))
        SET IDENTITY_INSERT [Brands] ON;
    EXEC(N'INSERT INTO [Brands] ([Id], [Country], [CreatedAt], [Name])
    VALUES (1, N''США'', ''2026-09-01T00:00:00.0000000Z'', N''CeraVe''),
    (2, N''Франція'', ''2026-09-01T00:00:00.0000000Z'', N''La Roche-Posay''),
    (3, N''Канада'', ''2026-09-01T00:00:00.0000000Z'', N''The Ordinary''),
    (4, N''Франція'', ''2026-09-01T00:00:00.0000000Z'', N''Bioderma''),
    (5, N''США'', ''2026-09-01T00:00:00.0000000Z'', N''Neutrogena''),
    (6, N''Франція'', ''2026-09-01T00:00:00.0000000Z'', N''Vichy''),
    (7, N''Швейцарія'', ''2026-09-01T00:00:00.0000000Z'', N''Cetaphil''),
    (8, N''Франція'', ''2026-09-01T00:00:00.0000000Z'', N''Avène'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'CreatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Brands]'))
        SET IDENTITY_INSERT [Brands] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Name', N'Slug', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [CreatedAt], [Name], [Slug], [SortOrder])
    VALUES (1, ''2026-09-01T00:00:00.0000000Z'', N''Очищення'', N''cleansing'', 1),
    (2, ''2026-09-01T00:00:00.0000000Z'', N''Зволоження'', N''moisturizing'', 2),
    (3, ''2026-09-01T00:00:00.0000000Z'', N''Сироватки'', N''serums'', 3),
    (4, ''2026-09-01T00:00:00.0000000Z'', N''Сонцезахист'', N''sun-protection'', 4),
    (5, ''2026-09-01T00:00:00.0000000Z'', N''Проблемна шкіра'', N''acne-prone'', 5),
    (6, ''2026-09-01T00:00:00.0000000Z'', N''Чутлива шкіра'', N''sensitive'', 6)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'Name', N'Slug', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Brands_Name] ON [Brands] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductCategories_CategoryId_AddedAt] ON [ProductCategories] ([CategoryId], [AddedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductDetails_SkinType] ON [ProductDetails] ([SkinType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [ProductImages] ([ProductId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductImages_ProductId_Url] ON [ProductImages] ([ProductId], [Url]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ProductImages_MainPerProduct] ON [ProductImages] ([ProductId]) WHERE [IsMain] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Products_BrandId_Price] ON [Products] ([BrandId], [Price]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019110512_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019110512_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

