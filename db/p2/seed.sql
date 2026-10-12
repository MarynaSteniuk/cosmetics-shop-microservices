USE CosmeticsCatalogDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET IDENTITY_INSERT dbo.Brands ON;

INSERT dbo.Brands (Id, Name, Country)
SELECT v.Id, v.Name, v.Country
FROM (VALUES
    (1, N'CeraVe',         N'США'),
    (2, N'La Roche-Posay', N'Франція'),
    (3, N'The Ordinary',   N'Канада'),
    (4, N'Bioderma',       N'Франція'),
    (5, N'Neutrogena',     N'США'),
    (6, N'Vichy',          N'Франція'),
    (7, N'Cetaphil',       N'Швейцарія'),
    (8, N'Avène',          N'Франція')
) AS v (Id, Name, Country)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Brands AS b WHERE b.Id = v.Id);

SET IDENTITY_INSERT dbo.Brands OFF;
GO

SET IDENTITY_INSERT dbo.Categories ON;

INSERT dbo.Categories (Id, Name, Slug, SortOrder)
SELECT v.Id, v.Name, v.Slug, v.SortOrder
FROM (VALUES
    (1, N'Очищення',          N'cleansing',      1),
    (2, N'Зволоження',        N'moisturizing',   2),
    (3, N'Сироватки',         N'serums',         3),
    (4, N'Сонцезахист',       N'sun-protection', 4),
    (5, N'Проблемна шкіра',   N'acne-prone',     5),
    (6, N'Чутлива шкіра',     N'sensitive',      6)
) AS v (Id, Name, Slug, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories AS c WHERE c.Id = v.Id);

SET IDENTITY_INSERT dbo.Categories OFF;
GO

SET IDENTITY_INSERT dbo.Products ON;

INSERT dbo.Products (Id, BrandId, Sku, Name, Price)
SELECT v.Id, v.BrandId, v.Sku, v.Name, v.Price
FROM (VALUES
    (1, 1, N'CRV-HCL-236', N'CeraVe Hydrating Cleanser 236 ml',               520.00),
    (2, 2, N'LRP-EFD-040', N'La Roche-Posay Effaclar Duo+ 40 ml',             780.00),
    (3, 3, N'TOR-NIA-030', N'The Ordinary Niacinamide 10% + Zinc 1% 30 ml',   450.00),
    (4, 4, N'BDM-SH2-500', N'Bioderma Sensibio H2O 500 ml',                   610.00),
    (5, 5, N'NTG-HBW-050', N'Neutrogena Hydro Boost Water Gel 50 ml',         430.00),
    (6, 6, N'VCH-M89-050', N'Vichy Minéral 89 Serum 50 ml',                   890.00),
    (7, 7, N'CTP-MCR-250', N'Cetaphil Moisturizing Cream 250 g',              560.00),
    (8, 8, N'AVN-CLS-050', N'Avène Cleanance SPF50+ 50 ml',                   700.00)
) AS v (Id, BrandId, Sku, Name, Price)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products AS p WHERE p.Id = v.Id);

SET IDENTITY_INSERT dbo.Products OFF;
GO

INSERT dbo.ProductDetails (ProductId, Description, Ingredients, HowToUse, Volume, VolumeUnit, SkinType)
SELECT v.ProductId, v.Description, v.Ingredients, v.HowToUse, v.Volume, v.VolumeUnit, v.SkinType
FROM (VALUES
    (1, N'Зволожувальний очищувальний засіб з церамідами та гіалуроновою кислотою.',
        N'Aqua, Glycerin, Cetearyl Alcohol, Ceramide NP, Ceramide AP, Ceramide EOP, Sodium Hyaluronate, Cholesterol',
        N'Нанести на вологу шкіру, помасажувати, змити водою.', 236, N'ml', N'Dry'),
    (2, N'Коригувальний крем для проблемної шкіри, зменшує недоліки та сліди постакне.',
        N'Aqua, Glycerin, Niacinamide, Zinc PCA, Salicylic Acid, Piroctone Olamine',
        N'Наносити зранку та ввечері на очищену шкіру обличчя.', 40, N'ml', N'Oily'),
    (3, N'Сироватка з ніацинамідом і цинком для зменшення висипань і звуження пор.',
        N'Aqua, Niacinamide, Pentylene Glycol, Zinc PCA, Dimethyl Isosorbide',
        N'Кілька крапель на обличчя перед кремом, зранку та ввечері.', 30, N'ml', N'Oily'),
    (4, N'Міцелярна вода для чутливої шкіри, очищує та знімає макіяж.',
        N'Aqua, PEG-6 Caprylic/Capric Glycerides, Fructooligosaccharides, Mannitol, Xylitol, Rhamnose',
        N'Змочити ватний диск і очистити обличчя та очі, не змивати.', 500, N'ml', N'Sensitive'),
    (5, N'Легкий гель-крем з гіалуроновою кислотою для інтенсивного зволоження.',
        N'Aqua, Dimethicone, Glycerin, Sodium Hyaluronate',
        N'Наносити на очищену шкіру зранку та ввечері.', 50, N'ml', N'Combination'),
    (6, N'Гель-бустер з вулканічною водою та гіалуроновою кислотою для зміцнення шкіри.',
        N'Aqua, Glycerin, Sodium Hyaluronate, Butylene Glycol',
        N'Нанести 2 краплі на шкіру обличчя перед кремом.', 50, N'ml', N'All'),
    (7, N'Живильний крем для сухої та чутливої шкіри обличчя і тіла.',
        N'Aqua, Glycerin, Petrolatum, Dimethicone, Helianthus Annuus Seed Oil, Niacinamide, Panthenol',
        N'Наносити на шкіру за потреби, особливо після душу.', 250, N'g', N'Dry'),
    (8, N'Сонцезахисний флюїд з високим рівнем захисту для жирної та проблемної шкіри.',
        N'Avene Aqua, Glycerin, Methylene Bis-Benzotriazolyl Tetramethylbutylphenol, Silica',
        N'Наносити рівномірно перед виходом на сонце, оновлювати кожні 2 години.', 50, N'ml', N'Oily')
) AS v (ProductId, Description, Ingredients, HowToUse, Volume, VolumeUnit, SkinType)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductDetails AS d WHERE d.ProductId = v.ProductId);
GO

INSERT dbo.ProductCategories (ProductId, CategoryId, AddedAt)
SELECT v.ProductId, v.CategoryId, v.AddedAt
FROM (VALUES
    (1, 1, CAST('2026-09-01' AS DATETIME2)),
    (1, 6, CAST('2026-09-01' AS DATETIME2)),
    (2, 5, CAST('2026-09-02' AS DATETIME2)),
    (2, 2, CAST('2026-09-05' AS DATETIME2)),
    (3, 3, CAST('2026-09-03' AS DATETIME2)),
    (3, 5, CAST('2026-09-03' AS DATETIME2)),
    (4, 1, CAST('2026-09-04' AS DATETIME2)),
    (4, 6, CAST('2026-09-04' AS DATETIME2)),
    (5, 2, CAST('2026-09-06' AS DATETIME2)),
    (6, 3, CAST('2026-09-07' AS DATETIME2)),
    (6, 2, CAST('2026-09-10' AS DATETIME2)),
    (7, 2, CAST('2026-09-08' AS DATETIME2)),
    (7, 6, CAST('2026-09-08' AS DATETIME2)),
    (8, 4, CAST('2026-09-09' AS DATETIME2)),
    (8, 5, CAST('2026-09-12' AS DATETIME2))
) AS v (ProductId, CategoryId, AddedAt)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductCategories AS pc
                  WHERE pc.ProductId = v.ProductId AND pc.CategoryId = v.CategoryId);
GO

INSERT dbo.ProductImages (ProductId, Url, AltText, SortOrder, IsMain)
SELECT v.ProductId, v.Url, v.AltText, v.SortOrder, v.IsMain
FROM (VALUES
    (1, N'/images/products/1/main.jpg', N'CeraVe Hydrating Cleanser',       0, 1),
    (1, N'/images/products/1/back.jpg', N'CeraVe Hydrating Cleanser, склад', 1, 0),
    (2, N'/images/products/2/main.jpg', N'Effaclar Duo+',                    0, 1),
    (3, N'/images/products/3/main.jpg', N'Niacinamide 10% + Zinc 1%',        0, 1),
    (3, N'/images/products/3/texture.jpg', N'Текстура сироватки',           1, 0),
    (4, N'/images/products/4/main.jpg', N'Sensibio H2O',                     0, 1),
    (5, N'/images/products/5/main.jpg', N'Hydro Boost Water Gel',            0, 1),
    (6, N'/images/products/6/main.jpg', N'Minéral 89',                       0, 1),
    (7, N'/images/products/7/main.jpg', N'Cetaphil Moisturizing Cream',      0, 1),
    (8, N'/images/products/8/main.jpg', N'Cleanance SPF50+',                 0, 1)
) AS v (ProductId, Url, AltText, SortOrder, IsMain)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductImages AS i
                  WHERE i.ProductId = v.ProductId AND i.Url = v.Url);
GO
