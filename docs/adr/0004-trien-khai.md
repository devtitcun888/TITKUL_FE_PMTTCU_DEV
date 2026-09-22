# ADR 0004 — Baseline triển khai FE

- Trạng thái: Accepted
- Ngày: 22/09/2026
- Phạm vi: Bước 1

## Bối cảnh

Mỗi repository có một pipeline. Bước 1 chưa đóng gói container và chưa có reverse proxy thật.

## Quyết định

- GitHub Actions `ci` chạy restore, build Release, test và `scripts/scan-secrets.sh` trên `ubuntu-latest`, SDK `10.0.x`.
- Workflow chạy khi push `main`, `develop`, `feat/**` và khi mở pull request. Quyền workflow chỉ `contents: read`.
- Nhánh làm việc của Bước 1 là `develop`. Không push `main` và không force-push.
- Health: `GET /health/live` và `GET /health/ready`. Cả hai chỉ báo tiến trình phục vụ HTTP. Kiểm tra PostgreSQL và quyền ghi media được thêm khi các phụ thuộc đó tồn tại.
- Ngoài `Development`, bật HSTS và chuyển hướng HTTPS. Header `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options` và CSP bật mọi môi trường.
- TLS production nằm ở reverse proxy. Bước 1 không tạo image container.
- Rollback của bước này là revert commit nền. Không có migration.

## Hệ quả

CI remote chỉ chạy sau khi nhánh được push và GitHub Actions được phép chạy. Syntax workflow được kiểm tra cục bộ; kết quả run trên GitHub là bằng chứng riêng nếu có.
