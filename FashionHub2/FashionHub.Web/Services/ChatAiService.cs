using FashionHub.Web.Data;
using FashionHub.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FashionHub.Web.Services
{
    public class ChatAiService : IChatAiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatAiService> _logger;

        public ChatAiService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<ChatAiService> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GetResponseAsync(string userMessage, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Chào bạn, OWE có thể giúp gì cho bạn?";

            string rawMsg = userMessage.ToLower();
            var orderMatch = Regex.Match(rawMsg, @"(?:dh|#|đơn|don)\s*[:#]?\s*(\d+)");

            if (orderMatch.Success && int.TryParse(orderMatch.Groups[1].Value, out int orderId))
            {
                try
                {
                    var order = await _context.DonHangs
                        .Include(d => d.IdtrangThaiNavigation)
                        .FirstOrDefaultAsync(d => d.IddonHang == orderId);

                    if (order != null)
                    {
                        string status = order.IdtrangThai switch
                        {
                            0 => "⏳ Chờ xác nhận",
                            1 => "📦 Đã xác nhận",
                            2 => "🚚 Đang giao hàng",
                            3 => "✅ Hoàn thành",
                            4 => "❌ Đã hủy",
                            _ => "Không xác định"
                        };

                        return $"🧾 <b>Đơn hàng #{orderId}</b><br>" +
                               $"Trạng thái: <span class='text-primary fw-bold'>{status}</span><br>" +
                               $"Tổng tiền: {order.TongThanhToan:N0}đ<br>" +
                               $"Ngày đặt: {(order.NgayTao.HasValue ? order.NgayTao.Value.ToString("dd/MM/yyyy") : "N/A")}";
                    }
                    else
                    {
                        return $"Mình tìm không thấy đơn hàng số <b>#{orderId}</b> trong hệ thống. Bạn kiểm tra lại giúp mình nhé!";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                }
            }

            try
            {
                string aiResponse = await CallGeminiAIAsync(userMessage);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini AI");
                return "Hệ thống đang bận, bạn thử lại sau chút nhé!";
            }
        }

        private async Task<string> CallGeminiAIAsync(string userMsg)
        {
            var productListStr = new StringBuilder();

            var products = await _context.SanPhams
                .Where(p => p.TrangThai == true)
                .OrderByDescending(p => p.IdsanPham)
                .Take(20)
                .Select(p => new { p.IdsanPham, p.TenSanPham, p.Gia })
                .ToListAsync();

            if (products.Any())
            {
                productListStr.AppendLine("\n--- DANH SÁCH SẢN PHẨM (Kèm ID) ---");
                foreach (var p in products)
                {
                    productListStr.AppendLine($"[ID: {p.IdsanPham}] {p.TenSanPham} - {p.Gia:N0}đ");
                }
                productListStr.AppendLine("-------------------------------------------");
            }
            else
            {
                productListStr.AppendLine("(Hiện tại shop đang tạm hết hàng các mẫu)");
            }

            string systemPrompt = $@"
                Bạn là nhân viên tư vấn bán hàng chuyên nghiệp của FashionHub (OWE).
        
                THÔNG TIN SHOP:
                - Địa chỉ: 114, Lê Trọng Tấn, Tân Phú, TP.Hồ Chí Minh.
                - Hotline: 09123123.
                - Chính sách: Ship 30k toàn quốc. Đổi trả trong 7 ngày.

                DỮ LIỆU SẢN PHẨM:
                {productListStr}

                NHIỆM VỤ CỦA BẠN:
                1. Trả lời ngắn gọn, thân thiện, xưng hô 'Mình' và 'Bạn'.
                2. Khi khách hỏi mua hoặc cần tư vấn, HÃY GỢI Ý sản phẩm từ danh sách trên.
        
                3. QUAN TRỌNG: Khi gợi ý sản phẩm, bạn BẮT BUỘC phải hiển thị theo định dạng HTML sau cho từng sản phẩm:
                   <div class='mb-2 border-bottom pb-2'>
                       <b>Tên sản phẩm</b> - <span class='text-danger'>Giá tiền</span><br>
                       <a href='/Products/Details/ID_CUA_SAN_PHAM' target='_blank' class='btn btn-sm btn-primary mt-1' style='border-radius: 20px; font-size: 12px; padding: 5px 15px;'>Xem chi tiết</a>
                   </div>

                   (Thay ID_CUA_SAN_PHAM bằng số ID tương ứng trong danh sách [ID: ...]).

                4. Tư vấn size: 
                   - Dưới 55kg: Size S
                   - 55-65kg: Size M
                   - 65-75kg: Size L
                   - Trên 75kg: Size XL
            ";

            var apiKey = _configuration["GeminiAI:ApiKey"] ?? "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
            var apiUrl = _configuration["GeminiAI:ApiUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent";

            var requestData = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = systemPrompt + "\n\nKhách hàng hỏi: " + userMsg } } }
                }
            };

            var client = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{apiUrl}?key={apiKey}", jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API error: {StatusCode}", response.StatusCode);
                return $"AI đang bận (Lỗi: {response.StatusCode}). Bạn thử lại sau nhé!";
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            string? aiText = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;

            if (!string.IsNullOrEmpty(aiText))
            {
                aiText = aiText.Replace("\n", "");
            }

            return aiText ?? "Xin lỗi, mình chưa hiểu ý bạn lắm.";
        }
    }
}