namespace FashionHub.Web.Domain;

public static class OrderStatusIds
{
    public const int Pending = 0;
    public const int Confirmed = 1;
    public const int Shipping = 2;
    public const int Completed = 3;
    public const int Cancelled = 4;
}

public static class OrderStatusTransitions
{
    public static bool CanTransition(int currentStatus, int nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (OrderStatusIds.Pending, OrderStatusIds.Confirmed) => true,
            (OrderStatusIds.Pending, OrderStatusIds.Cancelled) => true,
            (OrderStatusIds.Confirmed, OrderStatusIds.Shipping) => true,
            (OrderStatusIds.Confirmed, OrderStatusIds.Cancelled) => true,
            (OrderStatusIds.Shipping, OrderStatusIds.Completed) => true,
            _ => false
        };

    public static IReadOnlyList<int> GetAllowedNextStatusIds(int currentStatus) =>
        currentStatus switch
        {
            OrderStatusIds.Pending =>
                [OrderStatusIds.Confirmed, OrderStatusIds.Cancelled],
            OrderStatusIds.Confirmed =>
                [OrderStatusIds.Shipping, OrderStatusIds.Cancelled],
            OrderStatusIds.Shipping =>
                [OrderStatusIds.Completed],
            _ => []
        };
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

public static class PaymentMethodCodes
{
    public const string CashOnDelivery = "COD";
    public const string VnPay = "VNPAY";
    public const string Momo = "MOMO";
}

public static class PaymentStatusIds
{
    public const byte Unpaid = 0;
    public const byte Pending = 1;
    public const byte Paid = 2;
    public const byte Failed = 3;
    public const byte Refunded = 4;
}

public static class InventoryChangeTypes
{
    public const string OrderPlaced = "ORDER_PLACED";
    public const string OrderCancelled = "ORDER_CANCELLED";
    public const string ManualImport = "MANUAL_IMPORT";
    public const string ManualAdjustment = "MANUAL_ADJUSTMENT";
}
