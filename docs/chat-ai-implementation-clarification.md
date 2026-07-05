# BÁO CÁO LÀM RÕ: CHAT AI IMPLEMENTATION

**Ngày:** 05/07/2026  
**Vấn đề:** Làm rõ công nghệ AI được sử dụng trong ChatController

---

## CÂU TRẢ LỜI RÕ RÀNG

### 1. Code hiện tại đang dùng công nghệ gì?

**TRẢ LỜI:** Code hiện tại trong `FashionHub.Web` **ĐANG GỌI GEMINI API (Google)**, KHÔNG PHẢI dùng Microsoft.ML hoặc ONNX Runtime cục bộ.

**BẰNG CHỨNG CỤ THỂ:**

#### Project CŨ (FashionHub):
```csharp
// File: FashionHub/Controllers/ChatController.cs
// Dòng 17-18:
private const string GEMINI_API_KEY = "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

// Dòng 155:
var response = await client.PostAsync($"{API_URL}?key={GEMINI_API_KEY}", httpContent);
```

#### Project MỚI (FashionHub.Web):
```csharp
// File: FashionHub2/FashionHub.Web/Services/ChatAiService.cs
// Dòng 140-141:
var apiKey = _configuration["GeminiAI:ApiKey"] ?? "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
var apiUrl = _configuration["GeminiAI:ApiUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent";

// Dòng 157:
var response = await client.PostAsync($"{apiUrl}?key={apiKey}", jsonContent);
```

**KẾT LUẬN:** Cả project cũ và mới **ĐỀU ĐANG GỌI GEMINI API** của Google, không có ONNX Runtime hoặc Microsoft.ML nào.

---

### 2. Vì sao lại dùng Gemini API thay vì ONNX?

**TRẢ LỜI:** Project GỐC (FashionHub) **ĐÃ DÙNG GEMINI API TỪ ĐẦU**, không phải là quyết định của quá trình migration.

**PHÂN TÍCH:**

1. **Project gốc không có ONNX Runtime:**
   - Không có file `.onnx` nào trong project
   - Không có reference đến `Microsoft.ML` hoặc `Microsoft.ML.OnnxRuntime`
   - `ChatController.cs` trong project cũ đã gọi Gemini API từ đầu

2. **Migration chỉ port y nguyên logic:**
   - ChatAiService trong project mới chỉ chuyển code từ `ChatController.cs` cũ sang service pattern
   - Logic gọi API, system prompt, và xử lý response giữ nguyên 100%
   - Chỉ thay đổi cú pháp từ ASP.NET MVC 5 sang ASP.NET Core

3. **Có thể đây là nhầm lẫn trong mô tả ban đầu:**
   - Có thể có một phiên bản trước đó của project đã dùng ONNX
   - Hoặc có một feature khác (như ImageFeatureService) đang dùng ONNX
   - Nhưng ChatController trong code hiện tại **CHẮC CHẮN** đang dùng Gemini API

---

### 3. API Key được lưu ở đâu?

**TRẢ LỜI:** 

#### Project CŨ:
- **HARD-CODED trực tiếp trong code** (line 17):
  ```csharp
  private const string GEMINI_API_KEY = "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
  ```
- ⚠️ **Vi phạm security best practice**

#### Project MỚI:
- **Đọc từ Configuration với fallback:**
  ```csharp
  var apiKey = _configuration["GeminiAI:ApiKey"] ?? "AIzaSyCwSDhu2KY92SEAnHvs1RgZnXAKsAVnHrE";
  ```
- Có thể config trong:
  - `appsettings.json`
  - `appsettings.Development.json`
  - Environment variables
  - User Secrets
- ⚠️ **Vẫn có fallback hard-coded** (nên xóa trong production)

**KHUYẾN NGHỊ:**
1. Xóa fallback hard-coded API key
2. Lưu API key trong:
   - **Development:** User Secrets (`dotnet user-secrets set "GeminiAI:ApiKey" "your-key"`)
   - **Production:** Environment variables hoặc Azure Key Vault

---

### 4. Liệu có cách nào khác khả thi không?

**TRẢ LỜI:** CÓ, nhưng mỗi cách có tradeoffs riêng:

#### OPTION A: Tiếp tục dùng Gemini API (Hiện tại)
**Ưu điểm:**
- ✅ Đã hoạt động tốt trong project gốc
- ✅ Không cần training model
- ✅ Response quality cao (Gemini 2.0 Flash)
- ✅ Dễ maintain và scale
- ✅ Không cần GPU/resources mạnh

**Nhược điểm:**
- ❌ Phụ thuộc API bên thứ 3
- ❌ Chi phí (Google tính phí theo request sau free tier)
- ❌ Cần internet connection
- ❌ Latency cao hơn local model
- ❌ Data privacy concerns (gửi chat ra ngoài)

#### OPTION B: Chuyển sang ONNX Runtime với local model
**Ưu điểm:**
- ✅ Hoàn toàn offline
- ✅ Không có chi phí API
- ✅ Latency thấp
- ✅ Data privacy (không gửi ra ngoài)

**Nhược điểm:**
- ❌ Cần có sẵn model .onnx phù hợp
- ❌ Cần train hoặc fine-tune model cho domain thời trang
- ❌ Response quality có thể thấp hơn Gemini
- ❌ Cần resources: RAM, CPU/GPU mạnh
- ❌ Phức tạp hơn trong deployment

#### OPTION C: Azure OpenAI Service
**Ưu điểm:**
- ✅ Enterprise-grade SLA
- ✅ Data privacy tốt hơn (Azure compliance)
- ✅ Tích hợp tốt với .NET

**Nhược điểm:**
- ❌ Chi phí cao hơn Gemini
- ❌ Cần Azure subscription
- ❌ Phụ thuộc API

#### OPTION D: Hybrid Approach
- **Gemini API** cho conversations phức tạp, tư vấn sản phẩm
- **Rule-based/ONNX** cho queries đơn giản: tra đơn hàng, info shop

---

## KẾT LUẬN & KHUYẾN NGHỊ

### Hiện trạng:
- ✅ **Chat hiện tại ĐANG DÙNG GEMINI API**, không phải ONNX
- ✅ Code đã được migrate đúng từ project gốc
- ⚠️ API key cần được bảo mật tốt hơn (xóa fallback hard-coded)

### Khuyến nghị:
1. **Ngắn hạn:** 
   - Giữ nguyên Gemini API (đã hoạt động tốt)
   - Xóa API key hard-coded, dùng User Secrets/Environment Variables
   - Monitor usage để kiểm soát chi phí

2. **Trung hạn:**
   - Implement caching cho responses phổ biến
   - Rate limiting để tránh abuse
   - Fallback logic khi API fail

3. **Dài hạn (nếu cần):**
   - Evaluate chi phí thực tế
   - Nếu traffic cao: cân nhắc hybrid approach hoặc self-hosted model
   - Nếu privacy quan trọng: chuyển sang ONNX/local model

### Sửa lại báo cáo trước:
Câu "AI chatbot với Gemini API" trong báo cáo migration-comparison là **ĐÚNG** và không cần sửa.