using FashionHub.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FashionHub.Controllers
{
    public class ChatController : Controller
    {
        private const string GEMINI_API_KEY = "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        [HttpPost]
        public async Task<JsonResult> GetResponse(string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
                return Json(new { success = false, response = "Chào bạn, OWE có thể giúp gì cho bạn?" });

            string rawMsg = userMessage.ToLower();
            var orderMatch = System.Text.RegularExpressions.Regex.Match(rawMsg, @"(?:dh|#|đơn|don)\s*[:#]?\s*(\d+)");

            if (orderMatch.Success)
            {
                try
                {
                    int orderId = int.Parse(orderMatch.Groups[1].Value);
                    using (var db = new QL_SHOPQUANAO_PROEntities())
                    {
                        var order = db.DonHangs.Find(orderId);
                        if (order != null)
                        {
                            string status = "";
                            switch (order.IDTrangThai)
                            {
                                case 0: status = "⏳ Chờ xác nhận"; break;
                                case 1: status = "📦 Đã xác nhận"; break;
                                case 2: status = "🚚 Đang giao hàng"; break;
                                case 3: status = "✅ Hoàn thành"; break;
                                case 4: status = "❌ Đã hủy"; break;
                            }

                            string response = $"🧾 <b>Đơn hàng #{orderId}</b><br>" +
                                              $"Trạng thái: <span class='text-primary fw-bold'>{status}</span><br>" +
                                              $"Tổng tiền: {order.TongThanhToan:N0}đ<br>" +
                                              $"Ngày đặt: {order.NgayTao:dd/MM/yyyy}";

                            return Json(new { success = true, response = response });
                        }
                        else
                        {

                            return Json(new { success = true, response = $"Mình tìm không thấy đơn hàng số <b>#{orderId}</b> trong hệ thống. Bạn kiểm tra lại giúp mình nhé!" });
                        }
                    }
                }
                catch
                {
                }
            }

            try
            {
                string aiResponse = await CallGeminiAI(userMessage);
                return Json(new { success = true, response = aiResponse });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, response = "Hệ thống đang bận, bạn thử lại sau chút nhé! (Lỗi: " + ex.Message + ")" });
            }
        }
        private async Task<string> CallGeminiAI(string userMsg)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            StringBuilder productListStr = new StringBuilder();

            using (var db = new QL_SHOPQUANAO_PROEntities())
            {
                // 1. CẬP NHẬT QUERY: Lấy thêm IDSanPham
                var products = db.SanPhams
                    .Where(p => p.TrangThai == true)
                    .OrderByDescending(p => p.IDSanPham)
                    .Take(20)
                    .Select(p => new { p.IDSanPham, p.TenSanPham, p.Gia }) // <--- Lấy thêm ID
                    .ToList();

                if (products.Any())
                {
                    productListStr.AppendLine("\n--- DANH SÁCH SẢN PHẨM (Kèm ID) ---");
                    foreach (var p in products)
                    {
                        // 2. CUNG CẤP ID CHO AI: Định dạng [ID: 123] Tên - Giá
                        productListStr.AppendLine($"[ID: {p.IDSanPham}] {p.TenSanPham} - {p.Gia:N0}đ");
                    }
                    productListStr.AppendLine("-------------------------------------------");
                }
                else
                {
                    productListStr.AppendLine("(Hiện tại shop đang tạm hết hàng các mẫu)");
                }
            }

            // 3. CẬP NHẬT PROMPT: Dạy AI cách tạo nút bấm HTML
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

            // 4. GỌI API GEMINI (Giữ nguyên logic của bạn)
            using (var client = new HttpClient())
            {
                var requestData = new
                {
                    contents = new[]
                    {
                new { parts = new[] { new { text = systemPrompt + "\n\nKhách hàng hỏi: " + userMsg } } }
            }
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{API_URL}?key={GEMINI_API_KEY}", httpContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"AI đang bận (Lỗi: {response.StatusCode}). Bạn thử lại sau nhé!";
                }

                var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseString);
                string aiText = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;

                if (!string.IsNullOrEmpty(aiText))
                {
                    // Chỉ thay thế xuống dòng, không thay thế ** vì ta đã bảo AI trả về HTML chuẩn rồi
                    aiText = aiText.Replace("\n", "");
                }

                return aiText ?? "Xin lỗi, mình chưa hiểu ý bạn lắm.";
            }
        }
    }
}