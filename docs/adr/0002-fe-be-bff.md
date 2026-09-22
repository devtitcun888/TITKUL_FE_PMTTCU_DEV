# ADR 0002 — Hướng xác thực FE sang BE

- Trạng thái: Accepted
- Ngày: 22/09/2026
- Phạm vi: Bước 1, chỉ chốt hướng

## Bối cảnh

Kiến trúc đã chốt: trình duyệt nói chuyện với FE; FE gọi BE theo server-to-server. Bước 1 chưa làm đăng nhập.

## Quyết định

- Hướng BFF được giữ: credential API nằm phía server FE. Trình duyệt không giữ access token trong local storage hay session storage.
- Khi làm auth ở Bước 4, cookie quản trị là `HttpOnly`, `Secure`, `SameSite=Lax`, kèm chống CSRF cho thao tác ghi.
- Nếu phiên FE-BE dùng JWT, access token ngắn hạn và refresh token chỉ ở server.
- API công khai không cấp token. Danh sách endpoint công khai lấy từ hợp đồng API khi tới bước nghiệp vụ.
- `Backend:BaseUrl` hiện là `https://be.invalid`. Biến môi trường `Backend__BaseUrl` thay giá trị này ở Staging/Production.
- Bước 1 không có endpoint đăng nhập, không phát cookie phiên và không gọi BE.

## Hệ quả

Chưa có mã auth để kiểm thử 401/403. Hướng trên là ràng buộc cho Bước 4, không phải thiết kế lại nghiệp vụ.
