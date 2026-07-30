namespace FashionHub.Web.Domain;

public static class OrderStatusIds
{
    public const int Pending = 0;
    public const int Confirmed = 1;
    public const int Shipping = 2;
    public const int Completed = 3;
    public const int Cancelled = 4;
}

public static class CouponTypes
{
    public const int FixedAmount = 1;
    public const int Percentage = 2;
}

public static class ShippingFees
{
    public const decimal Standard = 30000m;
}

public static class InventoryChangeTypes
{
    public const string OrderPlaced = "ORDER_PLACED";
    public const string OrderCancelled = "ORDER_CANCELLED";
    public const string ManualImport = "MANUAL_IMPORT";
    public const string ManualAdjustment = "MANUAL_ADJUSTMENT";
}
