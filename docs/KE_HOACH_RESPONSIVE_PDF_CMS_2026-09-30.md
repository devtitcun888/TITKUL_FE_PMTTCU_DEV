# Kế hoạch responsive Public, xem PDF và giao diện quản trị CMS

Ngày rà soát: 30/09/2026

## Mục tiêu

1. Bảo đảm các trang Public dùng được ở điện thoại hẹp, máy tính bảng và desktop; không có chữ bị xếp dọc, tràn ngang hoặc nút bị khuất.
2. Cho phép người dân đọc trực tiếp tệp PDF trong trang chi tiết văn bản, đồng thời vẫn có nút tải/xem riêng; các tệp Word/Excel tiếp tục tải xuống.
3. Chuẩn hóa giao diện các trang quản trị CMS theo cùng một hệ thống tiêu đề, thẻ nội dung, bộ lọc, bảng, biểu mẫu và thao tác; giữ nguyên quyền và luồng nghiệp vụ.

## Pha 1 — Kiểm tra và xử lý responsive module Public

### Phạm vi

Toàn bộ route Razor Pages trong `Pages`: trang chủ; tin tức và chi tiết; thông báo và chi tiết; sự kiện và chi tiết; văn bản; biểu mẫu; thư viện và chi tiết; Bình dân học vụ số; khóa học nghề; học liệu và chi tiết; câu lạc bộ và chi tiết; lịch; khảo sát, biểu mẫu khảo sát và trang cảm ơn; đăng ký lớp, hủy đăng ký và trang cảm ơn; tra cứu, liên hệ, theo dõi phản ánh, điểm danh, văn hóa–thể thao, tìm kiếm, riêng tư và trang lỗi.

### Cách rà soát

- Kiểm tra layout dùng chung, các stylesheet toàn cục và stylesheet theo trang.
- Đối chiếu các breakpoint đang có: 1000/900/768/760/600/520/480/420/390 px; rà lại trạng thái bố cục tại 320/375/480/768/1024/1440 px.
- Kiểm tra các nhóm có nguy cơ tràn: navbar/search, thông báo khẩn, bộ lọc nhiều trường, nội dung dài/HTML, ảnh/video/iframe, bảng, lịch tháng, thẻ lớp/sự kiện và nút thao tác.
- Giữ nguyên cấu trúc dữ liệu và hành vi route; chỉ đổi bố cục hiển thị khi không cần thay đổi nghiệp vụ.

### Phát hiện ban đầu

- Thông báo khẩn có bốn phần tử con nhưng media query điện thoại tạo lưới ba cột. Nhãn chiếm cột rộng và liên kết tiêu đề bị ép vào cột hẹp, dẫn đến chữ rơi từng ký tự như ảnh người dùng gửi.
- Bộ lọc Public dùng hàng flex; các trang văn bản có hai ô nhập và nút lọc, dễ bị thu hẹp trên màn hình nhỏ.
- Các module nội dung chính đã có breakpoint riêng; cần kiểm tra các lớp chung và các trường hợp có nội dung dài.
- Rà soát template không thấy nhiều kích thước nội dung cố định; ảnh nội dung, video và bản đồ đã có giới hạn theo khung. Lịch tháng và bảng rich text được giữ cuộn trong vùng chứa.
- Kiểm tra trực quan trang chủ ở viewport 390 px sau sửa: khung không tràn ngang; tiêu đề thông báo khẩn xuống dòng theo từ/cụm từ bình thường.

### Tiêu chí hoàn tất

- Thông báo khẩn đọc bình thường, không có cột chữ dọc.
- Bộ lọc chuyển thành các hàng trường/nút có chiều rộng chạm phù hợp trên màn hình nhỏ.
- Không có cuộn ngang ở toàn trang do các phần tử giao diện; bảng/biểu đồ thực sự cần chiều rộng riêng phải cuộn trong chính vùng chứa.
- Layout và điều hướng vẫn hoạt động ở desktop, tablet, điện thoại.

## Pha 2 — Xem PDF trong chi tiết văn bản

### Công việc

1. Thêm API đọc chi tiết công khai cho một văn bản đã xuất bản, chỉ trả các trường công khai và không trả `storage_key`/metadata lưu trữ.
2. Tạo route chi tiết `/van-ban/{id}`; bổ sung liên kết từ danh sách sang trang chi tiết.
3. Tạo endpoint cùng origin để phục vụ PDF dạng inline; kiểm tra trạng thái công khai và loại tệp trước khi trả về. File Word/Excel chỉ cung cấp tải xuống.
4. Hiển thị metadata văn bản và vùng đọc PDF có chiều cao linh hoạt; có nút mở tab mới/tải xuống và thông báo thay thế khi trình duyệt không hỗ trợ nhúng PDF.
5. Chỉ nới chính sách frame cho response PDF cùng origin; các trang còn lại giữ chính sách chống nhúng hiện hành.
6. Chuyển liên kết văn bản ở trang chủ và danh sách sang trang chi tiết để người dùng có một đường vào xem PDF thống nhất.

### Tiêu chí hoàn tất

- Văn bản chưa xuất bản/không tồn tại không truy cập được qua API chi tiết hoặc tệp.
- PDF mở trong trang chi tiết ở desktop; ở điện thoại vùng xem chiếm toàn chiều rộng và có lối mở/tải thay thế.
- Nội dung không phải PDF không bị nhúng như PDF.

## Pha 3 — Nâng cấp giao diện module quản trị CMS

### Trang trong phạm vi

- Bài viết: danh sách, chi tiết/chỉnh sửa, xem trước.
- Thông báo: danh sách, chi tiết/chỉnh sửa, xem trước.
- Sự kiện: danh sách, chi tiết/chỉnh sửa, xem trước.
- Văn bản, biểu mẫu, chuyên mục.
- Thư viện/album: danh sách, chi tiết, tải media/thêm video.
- Tải ảnh CMS được xử lý ngay trong form biên tập; `/admin/cms/image-upload` là handler JSON, không phải một trang giao diện độc lập.

### Hướng xử lý

- Dùng stylesheet CMS riêng, nạp trong layout admin để tránh ảnh hưởng Public và các module đào tạo.
- Chuẩn hóa tiêu đề trang, breadcrumb, trạng thái, thông báo lỗi/thành công, nút thao tác, bề mặt thẻ và khoảng cách.
- Bố trí trường biểu mẫu theo lưới hai cột trên desktop; nội dung dài, CKEditor, preview và vùng tải tệp chiếm toàn hàng; chuyển thành một cột trên tablet/điện thoại.
- Bảng có header rõ, hàng xen kẽ, các thao tác không làm vỡ chiều rộng; chuyển cuộn vào vùng bảng trên màn hình hẹp.
- Trang biên tập có giới hạn chiều rộng đọc hợp lý, khoảng trắng nhất quán và preview phân biệt rõ với form.
- Không thay đổi endpoint, permission, trạng thái xuất bản, kiểm duyệt, dữ liệu hoặc nội dung trường.

### Tiêu chí hoàn tất

- CMS dùng chung một hệ thống giao diện trên các trang danh sách và chi tiết.
- Form và bảng phù hợp với desktop; ở tablet/điện thoại không có tràn toàn trang.
- CKEditor, chọn ảnh, tải tệp, xem trước và các hành động hiện hữu vẫn còn nguyên.

## Nhật ký thực hiện

- [x] Pha 1: rà soát CSS/layout và template Public; sửa bố cục thông báo khẩn và bộ lọc màn hẹp; xác nhận trang chủ ở viewport 390 px không tràn ngang.
- [x] Pha 2: thêm API chi tiết chỉ trả văn bản đã xuất bản, trang `/van-ban/{id}`, PDF proxy same-origin có kiểm tra MIME/chữ ký PDF, mở tab mới/tải xuống và liên kết từ trang chủ/danh sách.
- [x] Pha 3: thêm bộ giao diện CMS dùng riêng, áp dụng cho danh sách và trang biên tập bài viết/thông báo/sự kiện, album/thư viện, biểu mẫu, văn bản và chuyên mục; bảng cuộn nội bộ và form chuyển một cột ở màn hẹp.
- [x] Biên dịch FE và BE thành công (0 cảnh báo, 0 lỗi) ở thư mục output tạm để không ghi đè các tiến trình ứng dụng đang chạy.
- [x] Kiểm tra cuối `git diff --check`; không phát hiện lỗi whitespace. Cảnh báo CRLF của Git là thông tin chuẩn hóa xuống dòng trên các file Razor hiện hữu.
