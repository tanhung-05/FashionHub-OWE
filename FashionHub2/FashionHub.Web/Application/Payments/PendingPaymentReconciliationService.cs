using FashionHub.Web.Application.Orders;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FashionHub.Web.Application.Payments;

public sealed record PaymentReconciliationSummary(
    int Checked,
    int Paid,
    int Expired,
    int Unchanged);

public interface IPendingPaymentReconciliationService
{
    Task<PaymentReconciliationSummary> ReconcileAsync(
        CancellationToken cancellationToken = default);
}

public sealed class PendingPaymentReconciliationService :
    IPendingPaymentReconciliationService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IVnPayService vnPayService;
    private readonly IOrderCancellationService orderCancellationService;
    private readonly VnPayOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PendingPaymentReconciliationService> logger;

    public PendingPaymentReconciliationService(
        ApplicationDbContext dbContext,
        IVnPayService vnPayService,
        IOrderCancellationService orderCancellationService,
        IOptions<VnPayOptions> options,
        TimeProvider timeProvider,
        ILogger<PendingPaymentReconciliationService> logger)
    {
        this.dbContext = dbContext;
        this.vnPayService = vnPayService;
        this.orderCancellationService = orderCancellationService;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<PaymentReconciliationSummary> ReconcileAsync(
        CancellationToken cancellationToken = default)
    {
        if (!vnPayService.IsConfigured)
        {
            return new PaymentReconciliationSummary(0, 0, 0, 0);
        }

        var cutoffUtc = timeProvider.GetUtcNow()
            .AddHours(-Math.Clamp(options.AbandonedPaymentHours, 1, 168))
            .UtcDateTime;
        var candidates = await dbContext.GiaoDichThanhToans
            .Include(transaction => transaction.IddonHangNavigation)
                .ThenInclude(order => order.ChiTietDonHangs)
            .Where(transaction =>
                transaction.CongThanhToan == PaymentMethodCodes.VnPay
                && transaction.TrangThai == PaymentStatusIds.Pending
                && transaction.NgayTao <= cutoffUtc
                && transaction.IddonHangNavigation.IdtrangThai == OrderStatusIds.Pending)
            .OrderBy(transaction => transaction.NgayTao)
            .Take(50)
            .ToListAsync(cancellationToken);

        var paid = 0;
        var expired = 0;
        var unchanged = 0;
        foreach (var transaction in candidates)
        {
            var result = await vnPayService.QueryTransactionAsync(
                transaction,
                "127.0.0.1",
                cancellationToken);
            if (!result.IsValidSignature)
            {
                unchanged++;
                logger.LogWarning(
                    "Skipped VNPAY reconciliation for {Reference}: {Message}",
                    transaction.MaThamChieu,
                    result.Message);
                continue;
            }

            var order = transaction.IddonHangNavigation;
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            if (result.IsPaid)
            {
                transaction.TrangThai = PaymentStatusIds.Paid;
                transaction.MaPhanHoi = result.ResponseCode;
                transaction.MaGiaoDichCong = result.GatewayTransactionNumber;
                transaction.MaNganHang = result.BankCode;
                transaction.NgayThanhToan = result.PaidAt ?? nowUtc;
                transaction.NgayCapNhat = nowUtc;
                order.TrangThaiThanhToan = PaymentStatusIds.Paid;
                order.NgayThanhToan = transaction.NgayThanhToan;
                order.NgayCapNhat = timeProvider.GetLocalNow().DateTime;
                paid++;
            }
            else if (result.CanExpire)
            {
                transaction.TrangThai = PaymentStatusIds.Failed;
                transaction.MaPhanHoi = string.IsNullOrWhiteSpace(result.ResponseCode)
                    ? "EXPIRED"
                    : result.ResponseCode;
                transaction.NgayCapNhat = nowUtc;
                order.TrangThaiThanhToan = PaymentStatusIds.Failed;
                await orderCancellationService.ApplyAsync(
                    order,
                    null,
                    $"VNPAY payment expired ({transaction.MaThamChieu})",
                    cancellationToken);
                expired++;
            }
            else
            {
                unchanged++;
                continue;
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                logger.LogInformation(
                    exception,
                    "VNPAY transaction {Reference} was reconciled concurrently",
                    transaction.MaThamChieu);
                dbContext.ChangeTracker.Clear();
                break;
            }
        }

        return new PaymentReconciliationSummary(
            candidates.Count,
            paid,
            expired,
            unchanged);
    }
}
