# Hướng Dẫn Dọn Dẹp Project Cũ

**Ngày tạo:** 29/07/2026  
**Mục đích:** Di chuyển project cũ FashionHub (.NET Framework 4.8) ra khỏi repo để giữ repo sạch

---

## Tại Sao Cần Dọn Dẹp?

Migration sang ASP.NET Core MVC (.NET 10) đã hoàn tất 98%. Project mới `FashionHub2/` đã sẵn sàng production. Project cũ `FashionHub/` không còn cần thiết trong repo nhưng nên giữ làm backup để tham khảo.

---

## Option 1: Move Ra Ngoài Repo (KHUYẾN NGHỊ)

### Bước 1: Tạo thư mục backup
```cmd
mkdir "E:\NĂM 3\CNPM\FashionHub_Legacy_Backup"
```

### Bước 2: Copy project cũ ra ngoài
```cmd
xcopy "E:\NĂM 3\CNPM\Fasssshionnnnnn\FashionHub" "E:\NĂM 3\CNPM\FashionHub_Legacy_Backup\FashionHub" /E /I /H
```

### Bước 3: Verify backup thành công
Kiểm tra folder backup có đầy đủ files không.

### Bước 4: Xóa folder cũ trong repo

**PowerShell:**
```powershell
Remove-Item -Path "FashionHub" -Recurse -Force
```

**CMD (nếu dùng cmd.exe):**
```cmd
rmdir /S /Q FashionHub
```

### Bước 5: Xóa solution file cũ

**PowerShell:**
```powershell
Remove-Item -Path "FashionHub.sln" -Force
```

**CMD:**
```cmd
del FashionHub.sln
```

### Bước 6: Commit changes
```cmd
git add .
git commit -m "chore: remove old .NET Framework project after successful migration"
git push
```

---

## Option 2: Chỉ Xóa Files Không Cần (Nếu muốn giữ FashionHub/)

### Xóa build artifacts

**PowerShell:**
```powershell
Remove-Item -Path "FashionHub\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "FashionHub\obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "FashionHub\packages" -Recurse -Force -ErrorAction SilentlyContinue
```

**CMD:**
```cmd
cd FashionHub
rmdir /S /Q bin
rmdir /S /Q obj
rmdir /S /Q packages
```

### Xóa temporary files

**PowerShell:**
```powershell
Get-ChildItem -Path "FashionHub" -Filter "*.user" -Recurse | Remove-Item -Force
Get-ChildItem -Path "FashionHub" -Filter "*.suo" -Recurse | Remove-Item -Force
```

**CMD:**
```cmd
cd FashionHub
del /S *.user
del /S *.suo
```

---

## Sau Khi Dọn Dẹp

### Verify FashionHub2 vẫn hoạt động
```cmd
cd FashionHub2\FashionHub.Web
dotnet build
dotnet run
```

### Update .gitignore (nếu chọn Option 1)
Không cần update vì đã xóa FashionHub/ khỏi repo.

### Restructure repo (tùy chọn)
Nếu muốn, có thể đổi tên FashionHub2/ → FashionHub/ sau khi xóa folder cũ:
```cmd
ren FashionHub2 FashionHub
```
⚠️ Lưu ý: Phải update git remote URLs và các reference trong CI/CD nếu có.

---

## Files Cần Giữ

Các files sau KHÔNG nên xóa khỏi repo:
- ✅ `docs/` - Tất cả documentation
- ✅ `FashionHub2/` - Project mới
- ✅ `.gitignore`
- ✅ `README.md`
- ✅ `DB_Fixed.sql` - Database backup
- ✅ `FashionHub-AI-Agent-Roadmap.md`
- ✅ `.clinerules/` - AI agent rules

---

## Rollback Plan

Nếu cần khôi phục project cũ:

### Từ backup folder (Option 1)
```cmd
xcopy "E:\NĂM 3\CNPM\FashionHub_Legacy_Backup\FashionHub" "E:\NĂM 3\CNPM\Fasssshionnnnnn\FashionHub" /E /I /H
```

### Từ git history
```cmd
git log --all --full-history -- FashionHub/
git checkout <commit-hash> -- FashionHub/
```

---

## Checklist

- [ ] Backup project cũ ra ngoài repo
- [ ] Verify backup thành công
- [ ] Xóa FashionHub/ khỏi repo
- [ ] Xóa FashionHub.sln
- [ ] Test FashionHub2 vẫn build được
- [ ] Test FashionHub2 vẫn run được
- [ ] Commit & push changes
- [ ] Update documentation (nếu cần)

---

## Câu Hỏi Thường Gặp

**Q: Có cần giữ FashionHub.sln không?**  
A: Không. File này chỉ dùng cho Visual Studio với project .NET Framework. FashionHub2 dùng `FashionHub2.slnx`.

**Q: Nếu cần tham khảo code cũ sau này?**  
A: Dùng backup folder hoặc git history. Tất cả code cũ vẫn có trong git commits trước đó.

**Q: Project mới có thể run độc lập không?**  
A: Có. FashionHub2/FashionHub.Web hoàn toàn độc lập, không phụ thuộc vào FashionHub/ cũ.

**Q: Database có ảnh hưởng gì không?**  
A: Không. Cả 2 project đều dùng chung database schema. Xóa code cũ không ảnh hưởng database.

---

**Khuyến nghị:** Thực hiện Option 1 để giữ repo sạch sẽ và chuyên nghiệp.