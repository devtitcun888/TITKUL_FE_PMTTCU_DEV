# ADR 0001 — Cấu trúc solution FE

- Trạng thái: Accepted
- Ngày: 22/09/2026
- Phạm vi: Bước 1

## Bối cảnh

Cổng thông tin và khu vực quản trị dùng chung một ứng dụng ASP.NET Core Razor Pages .NET 10. Thư mục vật lý giữ tên `TITKUL_FE_TTCN`. Tên sản phẩm và remote là PMTTCU.

## Quyết định

- Một solution `TITKUL.PMTTCU.slnx`.
- `src/TITKUL.PMTTCU.Web`: Razor Pages, trang nền và health check.
- `tests/TITKUL.PMTTCU.Web.Tests`: kiểm thử host qua `WebApplicationFactory`.
- Target `net10.0`. SDK tối thiểu `10.0.301`, `rollForward` `latestFeature`.
- Bước 1 không tạo Area `Admin`, `ApiClients`, ViewModel nghiệp vụ hay module CMS/học tập.
- Không reference `PN_HDSBE_Core`. FE không kết nối PostgreSQL.

## Hệ quả

Các project Portal/Admin và client BFF được thêm ở Bước 3 khi có contract lỗi và correlation. Solution này chỉ đủ để restore, build, test và phục vụ health.
