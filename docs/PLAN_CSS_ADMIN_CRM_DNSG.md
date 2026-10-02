# Plan CSS Admin CRM — theo FE Doanh Nhân SG

Ngày: 01/10/2026  
Nguồn thị giác: `D:\WORK\TITKUL\5. BACKUP DATA\TITKUL\6. DOANH NHAN SAI GON\FE_DoanhNhanSG`  
Đích: `DEV/TITKUL_FE_PMTTCU_DEV` — khu **CRM admin** (Razor Pages, không đổi stack).

## 1. Mục tiêu

Áp logic bố cục CRM của Doanh Nhân SG lên cổng quản trị PMTTCU:

- Sidebar trắng, mục chọn dạng pill xanh, thu gọn được.
- Canvas xanh nhạt `#eff6ff`.
- Header trang 64px, tiêu đề 22px đậm, nút pill.
- Bảng không kẻ dọc, header `#eef4fb`, hàng hover `#f8fbff`.
- KPI có dải accent trái.
- Đăng nhập căn giữa trên nền canvas.
- Mobile: thanh 56px + drawer trái.

Giữ nguyên route, quyền, form, API. Font tự host `Be Vietnam Pro` (gần Gilroy/Plus Jakarta), fallback Roboto. Không tải Google Fonts (CSP). Nút chạm tối thiểu 44px.

## 2. Token lấy từ DNSG, map sang PMTTCU

| Vai trò | DNSG | PMTTCU admin |
|---|---|---|
| Canvas | `#eff6ff` | `--crm-bg` |
| Surface | `#ffffff` | `--crm-surface` |
| Chữ | `#0f274f` / `#0f172a` | `--crm-ink` |
| Primary | `#1d4ed8` | `#2157d7` (token sản phẩm đã chốt) |
| Nút đặc | `#1e3a8a` | `--crm-btn` |
| Sidebar | trắng 288px / 72px | giữ số đo DNSG |
| Selected | gradient `#dbeafe → #eff6ff` | giữ |
| Radius card | 14px | `--crm-radius` |
| Nút | pill 999px | pill, min-height 44px |
| Shadow | `0 8px 24px rgba(15,39,79,.04)` | `--crm-shadow` |

## 3. Khung dùng chung (mọi trang admin)

```
┌──────── 288px ────────┬────────────── 1fr ────────────────┐
│ Logo TT  Tân Trụ      │ Breadcrumb › Trang hiện tại  [role] [Cổng] │
│ Cổng quản trị         ├──────────────────────────────────────────┤
│ [Tìm trong menu]      │                                          │
│ ● Tổng quan           │  H1 tiêu đề            [nút chính]       │
│ ▸ Đào tạo & người học │  mô tả ngắn                              │
│ ▸ Nội dung & DV công  │                                          │
│ ▸ Khảo sát & báo cáo  │  [KPI / toolbar / form / bảng]           │
│ ▸ Quản trị hệ thống   │                                          │
│ ───────────────────── │                                          │
│ Tài khoản / vai trò   │                                          │
│ [Đăng xuất]           │                                          │
└───────────────────────┴──────────────────────────────────────────┘
```

Năm khung trang:

| Khung | Dùng cho |
|---|---|
| A Auth | Đăng nhập |
| B Dashboard | Tổng quan |
| C Danh sách | Bảng + lọc + CTA |
| D Biểu mẫu / biên tập | Form 2 cột, editor full hàng |
| E Lịch / điểm danh | Lưới tháng, hàng học viên |

## 4. Kế hoạch từng trang

### 4.1 Khung A — Xác thực

| Route | Việc CSS |
|---|---|
| `/admin/dang-nhap` | Card 16px radius, logo TT, tiêu đề “Trung tâm Tân Trụ / Cổng quản trị”, input 44px bo 12px, nút pill primary. |
| `/admin/dang-xuat` | Không đổi luồng; nếu render body thì dùng cùng canvas. |
| `/admin/khong-quyen` | Card trắng, H1, nút pill về tổng quan. |

### 4.2 Khung B — Tổng quan

| Route | Việc CSS |
|---|---|
| `/admin/tong-quan` | Heading 22px; bộ lọc ngày pill; 4 KPI strip trái; panel trắng radius 14; donut giữ nguyên data. |
| `/admin` | Cùng token card/H1 với empty state. |

### 4.3 Khung C — Danh sách đào tạo

| Route | Việc CSS |
|---|---|
| `/admin/chuong-trinh` | Đã có heading + toolbar + bảng hiện đại: pill CTA, search 44px, status chip, cột thao tác. |
| `/admin/doi-tuong` | Legacy card → cùng header/filter/table. |
| `/admin/thon-ap` | Như đối tượng. |
| `/admin/phong-hoc` | Như đối tượng. |
| `/admin/cau-lac-bo` | Danh sách + CTA chi tiết. |
| `/admin/nhan-su` | Form trong details: inner card, không double-border. |
| `/admin/lop-hoc` | Bảng lớp, badge trạng thái pill. |
| `/admin/hoc-vien` | Filter nhiều trường → lưới 2–4 cột desktop, 1 cột mobile; bảng cuộn trong khung. |
| `/admin/ke-hoach-hoat-dong` | Toolbar + bảng + form details. |
| `/admin/pcgd-xmc` | Nhiều card con: khoảng cách 16px, cùng radius. |
| `/admin/xoa-mu-chu` | Form + bảng sổ. |
| `/admin/so-tai-san-tai-lieu` | 3 khối card. |

### 4.4 Khung D — Chi tiết / form đào tạo

| Route | Việc CSS |
|---|---|
| `/admin/lop-hoc/{id}` | Form 2 cột, block dài full hàng. |
| `/admin/hoc-vien/{id}` | Form hồ sơ. |
| `/admin/cau-lac-bo/{id}` | Form + lịch CLB. |
| `/admin/chung-nhan` | Form cấp + bảng. |
| `/admin/don-phep` | Form duyệt. |
| `/admin/bao-mat` | Form MFA, code `mono` trong card. |

### 4.5 Khung E — Lịch và điểm danh

| Route | Việc CSS |
|---|---|
| `/admin/lich-hoc` | Heading + summary 3 KPI; ô tháng radius; session chip xanh. |
| `/admin/diem-danh` | Hàng học viên 56px; nhóm radio pill; thanh lưu sticky. |

### 4.6 CMS — danh sách (C)

| Route | Việc CSS |
|---|---|
| `/admin/cms/chuyen-muc` | Bỏ double-card; H1 22px; bảng header `#eef4fb`. |
| `/admin/cms/bai-viet` | CTA “Viết bài mới” pill; ô tìm + bảng. |
| `/admin/cms/su-kien` | Như bài viết. |
| `/admin/cms/thong-bao` | Như bài viết. |
| `/admin/cms/van-ban` | Form tải + bảng. |
| `/admin/cms/bieu-mau` | Như văn bản. |
| `/admin/cms/album` | Lưới/bảng album. |
| `/admin/cms/menu` | Cây menu trong card. |
| `/admin/cau-hinh` | Form cấu hình 2 cột. |
| `/admin/lien-he` | Bảng phản ánh. |

### 4.7 CMS — biên tập (D)

| Route | Việc CSS |
|---|---|
| `/admin/cms/bai-viet/sua` | Form 2 cột; CKEditor full hàng; preview tách card. |
| `/admin/cms/su-kien/sua` | Như bài viết. |
| `/admin/cms/thong-bao/sua` | Như bài viết. |
| `/admin/cms/album/{id}` | Form + lưới media. |
| `/admin/lien-he/{id}` | Form xử lý phản ánh. |

### 4.8 Khảo sát và báo cáo

| Route | Khung | Việc CSS |
|---|---|---|
| `/admin/khao-sat` | C | Danh sách đợt. |
| `/admin/khao-sat/thiet-ke` | D | Câu hỏi, nav pill. |
| `/admin/khao-sat/xem-truoc` | D | Preview card. |
| `/admin/khao-sat/ket-qua` | C/B | Bảng + chart. |
| `/admin/khao-sat/so-sanh` | C | Bộ lọc + bảng. |
| `/admin/dieu-tra-nhu-cau` | C/D | Nhiều card con. |
| `/admin/bao-cao` | B/C | KPI + bảng báo cáo. |
| `/admin/bao-cao/thang` | C | Form kỳ + bảng Excel. |

### 4.9 Hệ thống

| Route | Khung | Việc CSS |
|---|---|---|
| `/admin/nguoi-dung` | C/D | Form tạo + bảng. |
| `/admin/vai-tro` | C/D | Matrix quyền trong table-scroll. |
| `/admin/nhat-ky` | C | Bảng 20 dòng. |
| `/admin/sao-luu` | C | Catalog file, không bịa số bản sao. |

## 5. Thứ tự triển khai

1. Token + `admin-crm.css` + nạp trong `_Layout` / `_AuthLayout`.
2. Shell: sidebar trắng, pill, user panel, collapse, drawer mobile.
3. Login.
4. Cascade: `.card`, `h1`, `button`, `table`, `input` trong `.admin-main`.
5. Dashboard KPI strip.
6. Education + CMS (gỡ double-card, pill CTA).
7. Lịch / điểm danh / báo cáo theo token chung.
8. Kiểm tra 390 / 768 / 1440, không tràn ngang.

## 6. Cấm

- Không đổi API, permission, schema, CKEditor data.
- Không thêm màn ngoài module hiện có.
- Không đưa React/Ant Design/Blazor vào FE này.
- Không Google Fonts.
- Không sidebar tối `#143244`.
- Không tự đánh dấu gate M8 PASSED.
