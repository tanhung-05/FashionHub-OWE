# Git Workflow

## Commit theo từng task
- Mỗi task hoàn thành phải có một commit riêng.
- Không gộp nhiều thay đổi không liên quan vào cùng một commit.
- Trước khi commit, kiểm tra lại các file đã thay đổi bằng `git status` và `git diff`.

## Commit message
- Dùng message ngắn gọn, rõ nghĩa, theo dạng Conventional Commits:
  - `feat: ...` cho chức năng mới.
  - `fix: ...` cho sửa lỗi.
  - `refactor: ...` cho tái cấu trúc không đổi hành vi.
  - `style: ...` cho chỉnh UI/CSS/format.
  - `docs: ...` cho tài liệu.
  - `chore: ...` cho cấu hình, tooling, việc phụ trợ.
- Ví dụ:
  - `feat: add cart offcanvas`
  - `fix: handle empty product image`
  - `refactor: move cart logic to service`

## Không commit
- Không commit file build hoặc output tạm:
  - `bin/`
  - `obj/`
  - `.vs/`
  - `packages/` nếu đã được quản lý bằng NuGet restore.
  - log, cache, file tạm của IDE.
- Không commit secret, connection string thật, API key, password.

## Trước khi hoàn thành task
- Đảm bảo project build được nếu có thay đổi code.
- Đảm bảo UI vẫn dùng tiếng Việt và đúng guideline.
- Chỉ commit những file liên quan trực tiếp đến task.