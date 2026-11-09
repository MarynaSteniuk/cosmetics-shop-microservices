\xef\xbb\xbfUSE CosmeticsOrdersDb;
GO

IF OBJECT_ID(N'dbo.ProcessedEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessedEvents (
        EventId     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcessedEvents PRIMARY KEY,
        EventType   NVARCHAR(200)    NOT NULL,
        ProcessedAt DATETIME2        NOT NULL CONSTRAINT DF_ProcessedEvents_ProcessedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF COL_LENGTH(N'dbo.Products', N'CatalogNameSyncedAt') IS NULL
    ALTER TABLE dbo.Products ADD CatalogNameSyncedAt DATETIME2 NULL;
GO

IF COL_LENGTH(N'dbo.Products', N'CatalogPriceSyncedAt') IS NULL
    ALTER TABLE dbo.Products ADD CatalogPriceSyncedAt DATETIME2 NULL;
GO
