using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Seeding;

public static class DbInitializer
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        if (!await db.Brands.AnyAsync(ct) || !await db.Categories.AnyAsync(ct))
            throw new InvalidOperationException("Reference data is missing: apply migrations before seeding");

        var existingSkus = await db.Products.Select(p => p.Sku).ToListAsync(ct);
        var missing = BuildProducts().Where(p => !existingSkus.Contains(p.Sku)).ToList();

        if (missing.Count == 0)
            return;

        db.Products.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }

    private static IEnumerable<Product> BuildProducts()
    {
        yield return Create(1, "CRV-HCL-236", "CeraVe Hydrating Cleanser 236 ml", 520m, 50,
            new ProductDetail("Зволожувальний очищувальний засіб з церамідами та гіалуроновою кислотою.",
                "Aqua, Glycerin, Cetearyl Alcohol, Ceramide NP, Ceramide AP, Ceramide EOP, Sodium Hyaluronate",
                "Нанести на вологу шкіру, помасажувати, змити водою.", 236, VolumeUnits.Milliliters, SkinTypes.Dry),
            [(1, Day(1)), (6, Day(1))]);

        yield return Create(2, "LRP-EFD-040", "La Roche-Posay Effaclar Duo+ 40 ml", 780m, 30,
            new ProductDetail("Коригувальний крем для проблемної шкіри.",
                "Aqua, Glycerin, Niacinamide, Zinc PCA, Salicylic Acid",
                "Наносити зранку та ввечері на очищену шкіру.", 40, VolumeUnits.Milliliters, SkinTypes.Oily),
            [(5, Day(2)), (2, Day(5))]);

        yield return Create(3, "TOR-NIA-030", "The Ordinary Niacinamide 10% + Zinc 1% 30 ml", 450m, 40,
            new ProductDetail("Сироватка з ніацинамідом і цинком для звуження пор.",
                "Aqua, Niacinamide, Pentylene Glycol, Zinc PCA",
                "Кілька крапель перед кремом.", 30, VolumeUnits.Milliliters, SkinTypes.Oily),
            [(3, Day(3)), (5, Day(3))]);

        yield return Create(4, "BDM-SH2-500", "Bioderma Sensibio H2O 500 ml", 610m, 25,
            new ProductDetail("Міцелярна вода для чутливої шкіри.",
                "Aqua, PEG-6 Caprylic/Capric Glycerides, Fructooligosaccharides, Mannitol",
                "Змочити ватний диск і очистити обличчя.", 500, VolumeUnits.Milliliters, SkinTypes.Sensitive),
            [(1, Day(4)), (6, Day(4))]);

        yield return Create(5, "NTG-HBW-050", "Neutrogena Hydro Boost Water Gel 50 ml", 430m, 35,
            new ProductDetail("Легкий гель-крем з гіалуроновою кислотою.",
                "Aqua, Dimethicone, Glycerin, Sodium Hyaluronate",
                "Наносити на очищену шкіру зранку та ввечері.", 50, VolumeUnits.Milliliters, SkinTypes.Combination),
            [(2, Day(6))]);

        yield return Create(6, "VCH-M89-050", "Vichy Minéral 89 Serum 50 ml", 890m, 4,
            new ProductDetail("Гель-бустер з вулканічною водою та гіалуроновою кислотою.",
                "Aqua, Glycerin, Sodium Hyaluronate, Butylene Glycol",
                "Нанести 2 краплі перед кремом.", 50, VolumeUnits.Milliliters, SkinTypes.All),
            [(3, Day(7)), (2, Day(10))]);

        yield return Create(7, "CTP-MCR-250", "Cetaphil Moisturizing Cream 250 g", 560m, 30,
            new ProductDetail("Живильний крем для сухої та чутливої шкіри.",
                "Aqua, Glycerin, Petrolatum, Dimethicone, Niacinamide, Panthenol",
                "Наносити за потреби, особливо після душу.", 250, VolumeUnits.Grams, SkinTypes.Dry),
            [(2, Day(8)), (6, Day(8))]);

        yield return Create(8, "AVN-CLS-050", "Avène Cleanance SPF50+ 50 ml", 700m, 3,
            new ProductDetail("Сонцезахисний флюїд для жирної та проблемної шкіри.",
                "Avene Aqua, Glycerin, Silica",
                "Наносити перед виходом на сонце, оновлювати кожні 2 години.", 50, VolumeUnits.Milliliters, SkinTypes.Oily),
            [(4, Day(9)), (5, Day(12))]);

        yield return Create(2, "LRP-CIC-040", "La Roche-Posay Cicaplast Baume B5+ 40 ml", 450m, 25,
            new ProductDetail("Відновлювальний бальзам для подразненої шкіри.",
                "Aqua, Panthenol, Glycerin, Madecassoside",
                "Наносити на подразнені ділянки двічі на день.", 40, VolumeUnits.Milliliters, SkinTypes.Sensitive),
            [(2, Day(14)), (6, Day(14))]);

        yield return Create(3, "TOR-HYA-030", "The Ordinary Hyaluronic Acid 2% + B5 30 ml", 420m, 60,
            new ProductDetail("Зволожувальна сироватка з гіалуроновою кислотою.",
                "Aqua, Sodium Hyaluronate, Panthenol, Pentylene Glycol",
                "Нанести на вологу шкіру перед кремом.", 30, VolumeUnits.Milliliters, SkinTypes.All),
            [(3, Day(16)), (2, Day(16))]);

        yield return Create(4, "BDM-SEB-200", "Bioderma Sébium Gel Moussant 200 ml", 480m, 18,
            new ProductDetail("Очищувальний гель для жирної шкіри.",
                "Aqua, Sodium Laureth Sulfate, Zinc Sulfate, Copper Sulfate",
                "Спінити на вологій шкірі, змити.", 200, VolumeUnits.Milliliters, SkinTypes.Oily),
            [(1, Day(18)), (5, Day(18))]);

        yield return Create(6, "VCH-CSU-040", "Vichy Capital Soleil UV-Age SPF50+ 40 ml", 820m, 12,
            new ProductDetail("Сонцезахисний флюїд проти фотостаріння.",
                "Aqua, Glycerin, Niacinamide, Silica",
                "Наносити щоранку останнім кроком догляду.", 40, VolumeUnits.Milliliters, SkinTypes.All),
            [(4, Day(20))]);
    }

    private static Product Create(int brandId, string sku, string name, decimal price, int stock,
        ProductDetail detail, (int CategoryId, DateTime AddedAt)[] categories)
    {
        var product = new Product(brandId, sku, name, price);
        product.AddStock(stock);
        product.SetDetail(detail);
        product.AddImage($"/images/products/{sku.ToLowerInvariant()}/main.jpg", name, isMain: true);

        foreach (var (categoryId, addedAt) in categories)
            product.AddToCategory(categoryId, addedAt);

        return product;
    }

    private static DateTime Day(int day) => new(2026, 9, day, 0, 0, 0, DateTimeKind.Utc);
}
