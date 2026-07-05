# TRẠNG THÁI: ProductsController.SearchByImage

**Ngày:** 05/07/2026  
**Vấn đề:** Làm rõ tại sao SearchByImage bị đánh dấu "COPIED BUT DISABLED"

---

## HIỆN TRẠNG

**Action SearchByImage trong FashionHub.Web KHÔNG HOẠT ĐỘNG.**

### Bằng chứng từ code:

```csharp
// File: FashionHub2/FashionHub.Web/Controllers/ProductsController.cs
// Lines 167-172:

[HttpPost]
public IActionResult SearchByImage(IFormFile? imageFile)
{
    // Image search feature disabled to avoid dependency on local AI model or external services.
    // To re-enable, restore the original implementation in this method.
    return RedirectToAction("Index", new { error = "Tính năng tìm kiếm bằng hình ảnh đã bị tắt để ứng dụng hoạt động ổn định." });
}
```

**Comment rõ ràng:** "Image search feature disabled to avoid dependency on local AI model or external services."

---

## TẠI SAO BỊ DISABLE?

### 1. Phụ thuộc vào GenerateImageFeatures

SearchByImage cần:
- **Đặc trưng ảnh (image features)** đã được sinh trước và lưu trong database
- Mỗi sản phẩm cần có vector đặc trưng để so sánh với ảnh upload

**Workflow đúng:**
1. Admin upload ảnh sản phẩm
2. Admin chạy `Admin/ProductsController.GenerateImageFeatures` để sinh vector cho tất cả ảnh
3. Vectors được lưu vào cột `ImageFeatures` trong database
4. Khi khách tìm kiếm bằng ảnh: sinh vector cho ảnh upload → so sánh với vectors trong DB → trả về sản phẩm tương tự

**Vấn đề hiện tại:**
- `Admin/ProductsController.GenerateImageFeatures` **CHƯA ĐƯỢC MIGRATE**
- Không có vectors nào trong database
- SearchByImage không thể hoạt động vì không có gì để so sánh

### 2. Phụ thuộc vào ImageFeatureService

Mặc dù `ImageFeatureService.cs` đã có trong project mới, nhưng:
- Service này cần ONNX Runtime + model .onnx
- Cần cấu hình model path, input/output tensor names
- Cần test kỹ trước khi enable

### 3. Quyết định tạm thời disable

Để tránh:
- ❌ Runtime errors khi không có image features
- ❌ Null reference exceptions
- ❌ Confusing user experience (search không trả kết quả)

Code đã được **intentionally disabled** với message rõ ràng cho user.

---

## DEPENDENCIES ĐỂ ENABLE LẠI

### Bắt buộc phải có:
1. ✅ `ImageFeatureService` (đã có)
2. ✅ `IImageFeatureService` interface (đã có)
3. ❌ **Admin/ProductsController.GenerateImageFeatures** (CHƯA MIGRATE)
4. ❌ ONNX model file (e.g., `mobilenet_v2.onnx`)
5. ❌ Model configuration trong `appsettings.json`
6. ❌ Image features đã được generate cho products hiện có

### Các bước để enable:
1. Migrate `Admin/ProductsController.GenerateImageFeatures`
2. Cấu hình ONNX model path
3. Admin chạy GenerateImageFeatures cho tất cả sản phẩm
4. Test ImageFeatureService hoạt động đúng
5. Restore implementation trong SearchByImage
6. Test end-to-end search by image flow

---

## SO SÁNH VỚI PROJECT CŨ

### Project CŨ (FashionHub):
```csharp
// SearchByImage hoạt động đầy đủ với:
// 1. ONNX Runtime
// 2. GenerateImageFeatures action
// 3. Database có image features
```

### Project MỚI (FashionHub.Web):
```csharp
// SearchByImage bị disable vì:
// 1. GenerateImageFeatures chưa migrate
// 2. Database chưa có image features
// 3. Chưa test ONNX Runtime trên .NET 10
```

---

## ĐÁNH GIÁ TRONG BÁO CÁO MIGRATION

### ❌ SAI nếu ghi:
- "SearchByImage: ✅ MIGRATED"

### ✅ ĐÚNG phải ghi:
- "SearchByImage: ⚠️ COPIED BUT DISABLED (non-functional)"

**Lý do:**
- Code action đã copy sang project mới
- Nhưng bị **intentionally disabled** với redirect + error message
- Không thể hoạt động cho đến khi migrate GenerateImageFeatures và setup ONNX

---

## KẾ HOẠCH

### Giai đoạn 3 (Core migration - hiện tại):
- ✅ SearchByImage action đã copy
- ⚠️ Tạm disable để app ổn định
- ✅ Documented rõ ràng tại sao disable

### Giai đoạn 4 (Advanced features):
- [ ] Migrate Admin/ProductsController.GenerateImageFeatures
- [ ] Setup ONNX Runtime + model
- [ ] Test ImageFeatureService
- [ ] Generate features cho products hiện có
- [ ] Enable SearchByImage
- [ ] Test end-to-end

**Ước tính thời gian:** 1-2 ngày cho SearchByImage feature hoàn chỉnh (sau khi hoàn tất Giai đoạn 3)

---

## KẾT LUẬN

✅ **Quyết định disable SearchByImage là ĐÚNG ĐẮN** vì:
1. Tránh runtime errors
2. User experience rõ ràng (có thông báo tính năng tắt)
3. Cho phép app chạy ổn định với các features khác
4. Sẽ được enable lại trong Giai đoạn 4

⚠️ **Báo cáo migration phải CHÍNH XÁC:** 
- Không ghi "MIGRATED" cho feature không hoạt động
- Ghi rõ "COPIED BUT DISABLED" và lý do