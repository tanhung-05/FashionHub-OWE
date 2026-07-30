namespace FashionHub.Web.Application.Chat;

public sealed class ChatFaqCatalog : IChatFaqProvider
{
    private static readonly IReadOnlyList<ChatFaqEntry> Entries =
    [
        new(
            "returns",
            ["doi tra", "doi hang", "tra hang", "hoan hang"],
            "OWE hỗ trợ đổi sản phẩm trong 07 ngày kể từ khi nhận hàng nếu sản phẩm còn nguyên tem, chưa sử dụng và chưa giặt ủi. Bạn cần liên hệ OWE trước khi gửi sản phẩm.",
            "Xem chính sách đổi trả",
            "/Home/Privacy#doi-tra"),
        new(
            "shipping",
            ["phi ship", "phi van chuyen", "giao hang", "van chuyen", "ship"],
            "Phí vận chuyển tiêu chuẩn hiện tại là 30.000 ₫ cho mỗi đơn. Phí được xác nhận ở bước thanh toán.",
            "Xem giỏ hàng",
            "/Cart"),
        new(
            "payment",
            ["thanh toan", "cod", "chuyen khoan"],
            "Các phương thức thanh toán khả dụng được lấy trực tiếp từ hệ thống và hiển thị ở bước thanh toán.",
            "Đi đến thanh toán",
            "/Order/Checkout"),
        new(
            "coupon",
            ["ma giam gia", "coupon", "ma khuyen mai", "san ma"],
            "Mã giảm giá chỉ áp dụng khi còn hiệu lực, còn lượt dùng và đơn hàng đạt giá trị tối thiểu của chương trình.",
            "Xem giỏ hàng",
            "/Cart"),
        new(
            "size",
            ["chon size", "tu van size", "bang size", "kich thuoc"],
            "Hệ thống hiện chỉ có tên size theo từng biến thể sản phẩm, chưa có bảng số đo theo chiều cao hoặc cân nặng. Mình sẽ không tự suy đoán size; bạn hãy đối chiếu các size còn hàng trên trang chi tiết.",
            "Xem sản phẩm",
            "/Products"),
        new(
            "account",
            ["tai khoan", "dia chi", "doi mat khau", "ho so"],
            "Bạn có thể quản lý hồ sơ, địa chỉ giao hàng và lịch sử đơn trong khu vực tài khoản.",
            "Quản lý tài khoản",
            "/Account/Profile")
    ];

    public ChatFaqEntry? Find(string normalizedMessage)
    {
        return Entries.FirstOrDefault(entry =>
            entry.Keywords.Any(keyword =>
                normalizedMessage.Contains(keyword, StringComparison.Ordinal)));
    }
}
