using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Chat;

public sealed partial class ChatContextProvider : IChatContextProvider
{
    private const string ProductPlaceholder = "/images/products/aothun1_den_boxy.jpg";

    private static readonly IReadOnlyList<(string Trigger, string[] SearchTerms)> ProductMappings =
    [
        ("ao so mi", ["so mi"]),
        ("so mi", ["so mi"]),
        ("quan jeans", ["jeans"]),
        ("quan jean", ["jean"]),
        ("jeans", ["jeans"]),
        ("ao polo", ["polo"]),
        ("polo", ["polo"]),
        ("ao thun", ["ao thun"]),
        ("quan tay", ["quan tay"]),
        ("blazer", ["blazer"]),
        ("ao khoac", ["ao khoac"])
    ];

    private static readonly string[] ProductSignals =
    [
        "tim ao", "tim quan", "tim san pham", "co ao", "co quan",
        "goi y trang phuc", "mac di lam", "mac di phong van",
        "san pham nao", "dang giam gia", "sale", "khuyen mai"
    ];

    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserService currentUser;
    private readonly IChatFaqProvider faqProvider;

    public ChatContextProvider(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IChatFaqProvider faqProvider)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.faqProvider = faqProvider;
    }

    public async Task<ChatGroundingContext> GetContextAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        CancellationToken cancellationToken = default)
    {
        var normalized = ChatText.Normalize(message);

        if (ChatText.IsSecuritySensitive(message))
        {
            return new ChatGroundingContext(
                ChatIntentKind.SecurityRefusal,
                """{"security":"refused"}""",
                "Mình không thể cung cấp chỉ dẫn hệ thống, khóa API, cookie, mật khẩu hoặc dữ liệu nội bộ. Mình vẫn có thể hỗ trợ bạn tìm sản phẩm, xem chính sách và tra cứu đơn hàng của chính bạn.",
                [],
                null,
                [],
                UseAi: false);
        }

        if (IsOrderRequest(normalized))
        {
            return await BuildOrderContextAsync(
                normalized,
                history,
                cancellationToken);
        }

        var searchTerms = GetProductSearchTerms(normalized);
        var isScenarioRequest = normalized.Contains("phong van", StringComparison.Ordinal)
            || normalized.Contains("di lam", StringComparison.Ordinal)
            || normalized.Contains("cong so", StringComparison.Ordinal);
        var saleOnly = normalized.Contains("giam gia", StringComparison.Ordinal)
            || normalized.Contains("khuyen mai", StringComparison.Ordinal)
            || normalized.Contains("sale", StringComparison.Ordinal);
        var isProductRequest = searchTerms.Count > 0
            || isScenarioRequest
            || saleOnly
            || ProductSignals.Any(normalized.Contains);

        if (isProductRequest)
        {
            if (isScenarioRequest && searchTerms.Count == 0)
            {
                searchTerms = normalized.Contains("phong van", StringComparison.Ordinal)
                    ? ["so mi", "quan tay", "blazer", "vest"]
                    : ["polo", "so mi", "quan tay"];
            }

            return await BuildProductContextAsync(
                normalized,
                searchTerms,
                saleOnly,
                cancellationToken);
        }

        var faq = faqProvider.Find(normalized);
        if (faq != null)
        {
            return await BuildFaqContextAsync(faq, cancellationToken);
        }

        return new ChatGroundingContext(
            ChatIntentKind.General,
            """{"scope":["products","orders","faq"]}""",
            "Mình là Trợ lý OWE. Bạn có thể nhờ mình tìm sản phẩm theo giá, màu và size, xem ưu đãi, hỏi chính sách hoặc kiểm tra đơn hàng sau khi đăng nhập.",
            [],
            null,
            [
                new ChatActionDto("Xem sản phẩm", "/Products")
            ],
            UseAi: true);
    }

    private async Task<ChatGroundingContext> BuildProductContextAsync(
        string normalized,
        IReadOnlyList<string> searchTerms,
        bool saleOnly,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var candidates = await dbContext.SanPhams
            .AsNoTracking()
            .Where(product => product.TrangThai && product.DeletedAt == null)
            .OrderByDescending(product => product.NgayTao)
            .Take(150)
            .Include(product => product.IddanhMucNavigation)
            .Include(product => product.BienTheSanPhams)
                .ThenInclude(variant => variant.IdmauSacNavigation)
            .Include(product => product.BienTheSanPhams)
                .ThenInclude(variant => variant.IdkichThuocNavigation)
            .Include(product => product.BienTheSanPhams)
                .ThenInclude(variant => variant.HinhAnhBienThes)
                    .ThenInclude(link => link.IdhinhAnhNavigation)
            .ToListAsync(cancellationToken);

        var requestedColor = FindRequestedColor(normalized, candidates);
        var requestedSize = FindRequestedSize(normalized);
        var (minimumPrice, maximumPrice) = FindPriceRange(normalized);

        var matches = candidates
            .Select(product => new
            {
                Product = product,
                ActiveVariants = product.BienTheSanPhams
                    .Where(variant =>
                        variant.TrangThai
                        && variant.DeletedAt == null
                        && variant.SoLuongTon > 0)
                    .ToList(),
                EffectivePrice = IsSaleActive(product, now)
                    ? product.GiaKhuyenMai!.Value
                    : product.Gia,
                IsOnSale = IsSaleActive(product, now)
            })
            .Where(item => item.ActiveVariants.Count > 0)
            .Where(item =>
            {
                if (searchTerms.Count == 0)
                {
                    return true;
                }

                var haystack = ChatText.Normalize(string.Join(
                    " ",
                    item.Product.TenSanPham,
                    item.Product.MoTa,
                    item.Product.IddanhMucNavigation?.TenDanhMuc));
                return searchTerms.Any(term =>
                    haystack.Contains(term, StringComparison.Ordinal));
            })
            .Where(item => !saleOnly || item.IsOnSale)
            .Where(item => !minimumPrice.HasValue || item.EffectivePrice >= minimumPrice.Value)
            .Where(item => !maximumPrice.HasValue || item.EffectivePrice <= maximumPrice.Value)
            .Where(item => item.ActiveVariants.Any(variant =>
                (requestedColor == null
                    || ChatText.Normalize(variant.IdmauSacNavigation?.TenMau ?? string.Empty)
                        == requestedColor)
                && (requestedSize == null
                    || string.Equals(
                        variant.IdkichThuocNavigation?.TenKichThuoc,
                        requestedSize,
                        StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(item => item.IsOnSale)
            .ThenBy(item => item.EffectivePrice)
            .Take(ChatLimits.MaxProducts)
            .Select(item => MapProduct(item.Product, item.ActiveVariants, item.EffectivePrice, item.IsOnSale))
            .ToList();

        var safeResponse = matches.Count == 0
            ? "Mình chưa tìm thấy sản phẩm đang bán có biến thể còn hàng phù hợp với các điều kiện này. Bạn có thể thử nới khoảng giá, đổi màu hoặc chọn size khác."
            : $"Mình tìm thấy {matches.Count} sản phẩm phù hợp từ dữ liệu tồn kho hiện tại. Bạn hãy chọn đúng màu và size còn hàng trước khi thêm vào giỏ.";

        return new ChatGroundingContext(
            ChatIntentKind.ProductSearch,
            JsonSerializer.Serialize(new
            {
                intent = "product_search",
                filters = new
                {
                    searchTerms,
                    requestedColor,
                    requestedSize,
                    minimumPrice,
                    maximumPrice,
                    saleOnly
                },
                products = matches
            }),
            safeResponse,
            matches,
            null,
            matches.Count == 0
                ? [new ChatActionDto("Xem tất cả sản phẩm", "/Products")]
                : [],
            UseAi: true);
    }

    private async Task<ChatGroundingContext> BuildOrderContextAsync(
        string normalized,
        IReadOnlyList<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return new ChatGroundingContext(
                ChatIntentKind.OrderSupport,
                """{"authenticated":false}""",
                "Bạn cần đăng nhập để mình kiểm tra đơn hàng. Vì quyền riêng tư, mình chỉ có thể tra cứu đơn thuộc tài khoản đang đăng nhập.",
                [],
                null,
                [new ChatActionDto("Đăng nhập", "/Account/Login")],
                UseAi: false);
        }

        var userId = currentUser.UserId.Value;
        var wantsProcessingCount = normalized.Contains("bao nhieu", StringComparison.Ordinal)
            && (normalized.Contains("xu ly", StringComparison.Ordinal)
                || normalized.Contains("dang giao", StringComparison.Ordinal));
        var wantsCancel = normalized.Contains("huy", StringComparison.Ordinal);

        if (wantsProcessingCount)
        {
            var count = await dbContext.DonHangs
                .AsNoTracking()
                .CountAsync(order =>
                    order.IdnguoiDung == userId
                    && (order.IdtrangThai == OrderStatusIds.Pending
                        || order.IdtrangThai == OrderStatusIds.Confirmed
                        || order.IdtrangThai == OrderStatusIds.Shipping),
                    cancellationToken);

            var response = count == 0
                ? "Bạn hiện không có đơn nào đang xử lý."
                : $"Bạn hiện có {count} đơn đang xử lý.";
            return new ChatGroundingContext(
                ChatIntentKind.OrderSupport,
                JsonSerializer.Serialize(new { processingOrderCount = count }),
                response,
                [],
                null,
                [new ChatActionDto("Xem lịch sử đơn", "/Account/OrderHistory")],
                UseAi: false);
        }

        var requestedOrderId = FindOrderId(normalized)
            ?? (wantsCancel ? FindOrderIdInHistory(history) : null);

        var orders = dbContext.DonHangs
            .AsNoTracking()
            .Where(order => order.IdnguoiDung == userId)
            .Include(order => order.IdtrangThaiNavigation);

        DonHang? order;
        if (requestedOrderId.HasValue)
        {
            order = await orders.FirstOrDefaultAsync(
                item => item.IddonHang == requestedOrderId.Value,
                cancellationToken);
        }
        else
        {
            order = await orders
                .OrderByDescending(item => item.NgayTao)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (order == null)
        {
            var notFound = requestedOrderId.HasValue
                ? $"Không tìm thấy đơn #{requestedOrderId.Value} trong tài khoản của bạn."
                : "Tài khoản của bạn chưa có đơn hàng nào.";
            return new ChatGroundingContext(
                ChatIntentKind.OrderSupport,
                JsonSerializer.Serialize(new
                {
                    requestedOrderId,
                    found = false
                }),
                notFound,
                [],
                null,
                [new ChatActionDto("Xem lịch sử đơn", "/Account/OrderHistory")],
                UseAi: false);
        }

        var orderDto = MapOrder(order);
        var safeResponse = wantsCancel
            ? orderDto.CanCancel
                ? $"Đơn #{orderDto.Id} đang ở trạng thái “{orderDto.Status}” và có thể yêu cầu hủy trên trang chi tiết. Mình sẽ không tự hủy đơn khi chưa có bước xác nhận riêng."
                : $"Đơn #{orderDto.Id} đang ở trạng thái “{orderDto.Status}” nên hiện không thể hủy theo quy trình của hệ thống."
            : $"Đơn #{orderDto.Id} đang ở trạng thái “{orderDto.Status}”, đặt ngày {orderDto.CreatedAt:dd/MM/yyyy}, tổng thanh toán {orderDto.Total:N0} ₫.";

        return new ChatGroundingContext(
            ChatIntentKind.OrderSupport,
            JsonSerializer.Serialize(new
            {
                authenticated = true,
                order = orderDto
            }),
            safeResponse,
            [],
            orderDto,
            [new ChatActionDto("Xem chi tiết đơn", orderDto.DetailUrl)],
            UseAi: false);
    }

    private async Task<ChatGroundingContext> BuildFaqContextAsync(
        ChatFaqEntry faq,
        CancellationToken cancellationToken)
    {
        var answer = faq.Answer;
        object additionalContext = new { };

        if (faq.Id == "payment")
        {
            var methods = await dbContext.PhuongThucThanhToans
                .AsNoTracking()
                .Where(method => method.TrangThai)
                .OrderBy(method => method.IdphuongThucThanhToan)
                .Select(method => method.TenPhuongThuc)
                .ToListAsync(cancellationToken);
            answer = methods.Count == 0
                ? "Hệ thống chưa công bố phương thức thanh toán đang hoạt động."
                : $"Các phương thức thanh toán đang hoạt động: {string.Join(", ", methods)}.";
            additionalContext = new { paymentMethods = methods };
        }
        else if (faq.Id == "coupon")
        {
            var now = DateTime.Now;
            var coupons = await dbContext.MaGiamGia
                .AsNoTracking()
                .Where(coupon =>
                    coupon.TrangThai
                    && coupon.DeletedAt == null
                    && coupon.NgayBatDau <= now
                    && coupon.NgayKetThuc >= now
                    && coupon.DaSuDung < coupon.SoLuong)
                .OrderBy(coupon => coupon.NgayKetThuc)
                .Take(5)
                .Select(coupon => new
                {
                    coupon.MaCode,
                    coupon.TenChuongTrinh,
                    coupon.LoaiGiamGia,
                    coupon.GiaTri,
                    coupon.DonHangToiThieu,
                    coupon.GiamToiDa,
                    coupon.NgayKetThuc
                })
                .ToListAsync(cancellationToken);

            answer = coupons.Count == 0
                ? "Hiện chưa có mã giảm giá công khai còn hiệu lực trong hệ thống."
                : "Mã đang còn hiệu lực: " + string.Join(
                    "; ",
                    coupons.Select(coupon =>
                        $"{coupon.MaCode} – {FormatDiscount(coupon.LoaiGiamGia, coupon.GiaTri)}"
                        + (coupon.DonHangToiThieu > 0
                            ? $", đơn từ {coupon.DonHangToiThieu:N0} ₫"
                            : string.Empty)));
            additionalContext = new { coupons };
        }

        return new ChatGroundingContext(
            ChatIntentKind.Faq,
            JsonSerializer.Serialize(new
            {
                faq.Id,
                answer,
                source = faq.SourceUrl,
                additionalContext
            }),
            answer,
            [],
            null,
            [new ChatActionDto(faq.LinkLabel, faq.SourceUrl)],
            UseAi: true);
    }

    private static ChatProductDto MapProduct(
        SanPham product,
        IReadOnlyList<BienTheSanPham> variants,
        decimal effectivePrice,
        bool isOnSale)
    {
        var imagePath = variants
            .SelectMany(variant => variant.HinhAnhBienThes)
            .OrderByDescending(link => link.LaAnhChinh)
            .ThenBy(link => link.ThuTuHienThi)
            .Select(link => link.IdhinhAnhNavigation.DuongDan)
            .FirstOrDefault();
        var safeImagePath = !string.IsNullOrWhiteSpace(imagePath)
            && imagePath.StartsWith('/')
            && !imagePath.StartsWith("//", StringComparison.Ordinal)
                ? imagePath
                : ProductPlaceholder;

        return new ChatProductDto(
            product.IdsanPham,
            product.TenSanPham,
            safeImagePath,
            product.Gia,
            effectivePrice,
            isOnSale,
            $"/Products/Details/{product.IdsanPham}",
            variants
                .OrderBy(variant => variant.IdmauSacNavigation?.TenMau)
                .ThenBy(variant => variant.IdkichThuocNavigation?.TenKichThuoc)
                .Select(variant => new ChatProductVariantDto(
                    variant.IdbienThe,
                    variant.IdmauSacNavigation?.TenMau,
                    variant.IdkichThuocNavigation?.TenKichThuoc,
                    variant.SoLuongTon))
                .ToList());
    }

    private static ChatOrderDto MapOrder(DonHang order)
    {
        var status = order.IdtrangThaiNavigation?.TenTrangThai
            ?? order.IdtrangThai switch
            {
                OrderStatusIds.Pending => "Chờ xác nhận",
                OrderStatusIds.Confirmed => "Đã xác nhận",
                OrderStatusIds.Shipping => "Đang giao",
                OrderStatusIds.Completed => "Hoàn thành",
                OrderStatusIds.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };

        return new ChatOrderDto(
            order.IddonHang,
            status,
            order.TongThanhToan,
            order.NgayTao,
            order.IdtrangThai == OrderStatusIds.Pending,
            $"/Account/OrderDetail/{order.IddonHang}");
    }

    private static IReadOnlyList<string> GetProductSearchTerms(string normalized)
    {
        foreach (var mapping in ProductMappings)
        {
            if (normalized.Contains(mapping.Trigger, StringComparison.Ordinal))
            {
                return mapping.SearchTerms;
            }
        }

        return [];
    }

    private static string? FindRequestedColor(
        string normalized,
        IEnumerable<SanPham> products)
    {
        return products
            .SelectMany(product => product.BienTheSanPhams)
            .Select(variant => ChatText.Normalize(
                variant.IdmauSacNavigation?.TenMau ?? string.Empty))
            .Where(color => color.Length > 0)
            .Distinct()
            .OrderByDescending(color => color.Length)
            .FirstOrDefault(color =>
                normalized.Contains(color, StringComparison.Ordinal));
    }

    private static string? FindRequestedSize(string normalized)
    {
        var match = SizeRegex().Match(normalized);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static (decimal? Minimum, decimal? Maximum) FindPriceRange(string normalized)
    {
        var maximumMatch = MaximumPriceRegex().Match(normalized);
        var minimumMatch = MinimumPriceRegex().Match(normalized);
        return (
            minimumMatch.Success ? ParsePrice(minimumMatch) : null,
            maximumMatch.Success ? ParsePrice(maximumMatch) : null);
    }

    private static decimal? ParsePrice(Match match)
    {
        if (!decimal.TryParse(
                match.Groups[1].Value.Replace(',', '.'),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        var unit = match.Groups[2].Value;
        return unit switch
        {
            "trieu" or "tr" => amount * 1_000_000m,
            "nghin" or "ngan" or "k" => amount * 1_000m,
            _ when amount < 10_000m => amount * 1_000m,
            _ => amount
        };
    }

    private static bool IsOrderRequest(string normalized)
    {
        return normalized.Contains("don hang", StringComparison.Ordinal)
            || normalized.Contains("don #", StringComparison.Ordinal)
            || normalized.Contains("huy don", StringComparison.Ordinal);
    }

    private static int? FindOrderId(string normalized)
    {
        var match = OrderIdRegex().Match(normalized);
        return match.Success && int.TryParse(match.Groups[1].Value, out var orderId)
            ? orderId
            : null;
    }

    private static int? FindOrderIdInHistory(IReadOnlyList<ChatMessageDto> history)
    {
        foreach (var message in history.Reverse())
        {
            if (message.Order != null)
            {
                return message.Order.Id;
            }

            var match = HashIdRegex().Match(message.Content);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var orderId))
            {
                return orderId;
            }
        }

        return null;
    }

    private static bool IsSaleActive(SanPham product, DateTime now)
    {
        return product.GiaKhuyenMai.HasValue
            && product.GiaKhuyenMai.Value < product.Gia
            && product.NgayBatDauKm <= now
            && product.NgayKetThucKm >= now;
    }

    private static string FormatDiscount(int type, decimal value)
    {
        return type == CouponTypes.Percentage
            ? $"giảm {value:N0}%"
            : $"giảm {value:N0} ₫";
    }

    [GeneratedRegex(@"(?:size|kich thuoc)\s*[:\-]?\s*([a-z0-9]{1,4})\b")]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"(?:duoi|khong qua|toi da)\s*(\d+(?:[.,]\d+)?)\s*(trieu|tr|nghin|ngan|k)?")]
    private static partial Regex MaximumPriceRegex();

    [GeneratedRegex(@"(?:tren|tu)\s*(\d+(?:[.,]\d+)?)\s*(trieu|tr|nghin|ngan|k)?")]
    private static partial Regex MinimumPriceRegex();

    [GeneratedRegex(@"(?:don(?: hang)?(?: so)?|#|so)\s*#?\s*(\d+)\b")]
    private static partial Regex OrderIdRegex();

    [GeneratedRegex(@"#(\d+)\b")]
    private static partial Regex HashIdRegex();
}
