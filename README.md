# TITKUL_FE_PMTTCU

Cổng thông tin Razor Pages .NET 10 cho Trung tâm cung ứng dịch vụ sự nghiệp công xã Tân Trụ. Bước 1 chỉ gồm ứng dụng nền và health check.

## Yêu cầu

- .NET SDK 10, bản đã kiểm tra cục bộ: `10.0.301`

## Lệnh

```powershell
dotnet restore TITKUL.PMTTCU.slnx
dotnet build TITKUL.PMTTCU.slnx --configuration Release
dotnet test TITKUL.PMTTCU.slnx --configuration Release
bash scripts/scan-secrets.sh
```

## Health

- `GET /health/live`
- `GET /health/ready`

Hai endpoint trả JSON `status: Healthy` khi tiến trình phục vụ HTTP. Chưa kiểm tra cơ sở dữ liệu.

## Cấu hình

`Backend:BaseUrl` trong Git là `https://be.invalid`. Môi trường thật ghi đè bằng `Backend__BaseUrl`. Không commit mật khẩu, token hay connection string.

SEO canonical và Open Graph URL cần host public chuẩn. Cấu hình `Public:PortalBaseUrl` (biến môi trường `Public__PortalBaseUrl`) bằng origin chính thức, ví dụ `https://congdong.example.gov.vn`; Development đã đặt `http://localhost:5115`. Không lấy canonical host từ request để tránh ghi nhận Host header không đáng tin.

Quyết định đã chốt nằm ở `docs/adr/`.
