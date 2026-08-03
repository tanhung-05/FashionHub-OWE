namespace FashionHub.Web.ViewModels.Payment;

public sealed class PaymentResultViewModel
{
    public bool IsValidSignature { get; init; }

    public bool IsSuccessful { get; init; }

    public int? OrderId { get; init; }

    public string? TransactionReference { get; init; }

    public string ResponseCode { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
