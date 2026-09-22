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

Quyết định đã chốt nằm ở `docs/adr/`.
