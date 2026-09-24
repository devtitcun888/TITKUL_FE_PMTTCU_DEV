# Kế hoạch nâng cấp trang chủ — Cổng thông tin Trung tâm Tân Trụ

## Phạm vi đợt 1

Đợt này tập trung vào trang công khai `/` và app shell dùng chung. Không thay đổi API, database, authentication hoặc logic quản trị hiện có.

## Định hướng giao diện

- Màu chủ đạo: `#2157d7`.
- Nền sáng xanh-xám rất nhẹ, surface trắng, chữ navy đậm.
- Header hai tầng: utility bar cho hotline/email/địa chỉ và navigation chính cho tác vụ.
- Hero giới thiệu Trung tâm với CTA “Xem lớp đang mở đăng ký” và “Tra cứu chứng nhận”.
- Section “Truy cập nhanh” ưu tiên các tác vụ công khai có tần suất cao.
- Nội dung động hiện có tiếp tục lấy từ `/api/v1/public/home`: thông báo khẩn, tin ghim, tin mới, sự kiện sắp tới và học liệu.
- Nếu API chưa trả dữ liệu, hiển thị empty state rõ ràng thay vì tạo số liệu giả.
- Mobile chuyển navigation thành vùng cuộn ngang, hero một cột, các card về một cột hoặc hai cột tùy độ rộng.

## Kết quả dự kiến

1. Trang chủ có bố cục chuyên nghiệp, hiện đại và nhất quán với mockup trong `docs/images`.
2. Header/footer và focus state được nâng cấp nhưng không phá vỡ route hiện tại.
3. Các link hiện có tiếp tục trỏ về đúng Razor Page.
4. Chạy được build và kiểm tra `git diff --check`.

## Tiêu chí nghiệm thu

- [ ] Không có màu nâu/be trong trang chủ.
- [ ] Màu primary chính dùng `#2157d7`, hover dùng tông xanh đậm hơn.
- [ ] Có CTA rõ cho đăng ký lớp và tra cứu chứng nhận.
- [ ] Có quick access cho giới thiệu, lớp học, chứng nhận, văn bản/biểu mẫu, văn hóa–thể thao, khảo sát, học liệu và liên hệ.
- [ ] Các vùng nội dung động phân biệt rõ empty state và dữ liệu có thật.
- [ ] Responsive ở màn hình mobile và desktop.
- [ ] Không thay đổi backend hoặc contract API.

## Phạm vi tiếp theo đề xuất

Sau khi duyệt trang chủ, tiếp tục chuẩn hóa các trang danh sách (`Tin tức`, `Sự kiện`, `Học liệu`, `Thư viện`) theo cùng card, badge, filter và detail pattern.
