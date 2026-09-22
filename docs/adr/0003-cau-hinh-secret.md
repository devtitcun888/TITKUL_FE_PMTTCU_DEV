# ADR 0003 — Cấu hình và secret

- Trạng thái: Accepted
- Ngày: 22/09/2026
- Phạm vi: Bước 1

## Bối cảnh

Production secret đi qua environment hoặc secret manager. Repository không được chứa mật khẩu, token, connection string, CCCD hay binary Core.

## Quyết định

- Ba môi trường: `Development`, `Staging`, `Production`, chọn bằng `ASPNETCORE_ENVIRONMENT`.
- `appsettings.json` và `appsettings.{Environment}.json` chỉ chứa giá trị công khai hoặc placeholder.
- Địa chỉ BE placeholder là `https://be.invalid`. Không có khóa mật khẩu trong file cấu hình FE.
- File `appsettings.*.local.json`, `secrets.json`, `.env` và chứng chỉ `pfx`/`p12`/`pem` bị gitignore.
- `AllowedHosts` giữ `*` cho đến khi quyết định D-02 chốt domain.
- Giới hạn body hiện tại là 1 MiB vì chưa có endpoint upload. Bước upload sẽ nâng giới hạn theo loại tệp đã chốt.
- Ứng dụng Bước 1 không đọc secret cơ sở dữ liệu và không fail-fast khi thiếu mật khẩu PostgreSQL. Fail-fast bắt đầu khi tiến trình thực sự cần secret đó.

## Hệ quả

CI có secret scan. Giá trị thật chỉ được đưa qua environment trên máy triển khai, không commit.
