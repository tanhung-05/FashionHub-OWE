using FashionHub.Web.Application.Common;
using FashionHub.Web.Models.Generated;
using System.Text.Json.Serialization;

namespace FashionHub.Web.Application.Payments;

public sealed class VnPayOptions
{
    public const string SectionName = "VnPay";

    public string PaymentUrl { get; set; } =
        "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    public string TmnCode { get; set; } = string.Empty;

    public string HashSecret { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;

    public string TransactionApiUrl { get; set; } =
        "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";

    public int PaymentTimeoutMinutes { get; set; } = 15;

    public int AbandonedPaymentHours { get; set; } = 24;

    public int ReconciliationIntervalMinutes { get; set; } = 15;
}

public sealed record VnPayCallbackResult(
    bool IsValidSignature,
    bool IsSuccessful,
    bool IsAlreadyProcessed,
    int? OrderId,
    string? TransactionReference,
    string ResponseCode,
    string Message,
    string MerchantResponseCode);

public sealed record VnPayIpnResponse(
    [property: JsonPropertyName("RspCode")] string ResponseCode,
    [property: JsonPropertyName("Message")] string Message);

public sealed record VnPayQueryResult(
    bool IsValidSignature,
    bool IsRequestSuccessful,
    bool IsFound,
    string ResponseCode,
    string TransactionStatus,
    string Message,
    string? GatewayTransactionNumber,
    string? BankCode,
    DateTime? PaidAt)
{
    public bool IsPaid =>
        IsValidSignature
        && IsRequestSuccessful
        && TransactionStatus == "00";

    public bool CanExpire =>
        IsValidSignature
        && (ResponseCode == "91" || TransactionStatus is "01" or "02");
}

public interface IVnPayService
{
    bool IsConfigured { get; }

    Task<ServiceResult<string>> CreatePaymentUrlAsync(
        int orderId,
        int userId,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task<VnPayCallbackResult> ProcessCallbackAsync(
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken = default);

    Task<VnPayQueryResult> QueryTransactionAsync(
        GiaoDichThanhToan transaction,
        string ipAddress,
        CancellationToken cancellationToken = default);
}
