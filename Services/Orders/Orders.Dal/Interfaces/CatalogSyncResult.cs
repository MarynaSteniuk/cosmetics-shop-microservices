namespace Orders.Dal.Interfaces;

public enum CatalogSyncResult
{
    Updated,
    Inserted,
    Stale,
    NotFound
}
