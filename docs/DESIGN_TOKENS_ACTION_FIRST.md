# TITKUL Action-first Design Tokens

Bộ token này dùng cho cổng thông tin Trung tâm dịch vụ sự nghiệp công xã Tân Trụ, với màu chủ đạo **`#2157D7`**. Mục tiêu là giữ nhất quán giữa homepage public, các trang danh sách và cổng quản trị cán bộ.

## Quy ước theme

Light mode dùng nền trắng/xanh-xám rất sáng để tăng khả năng đọc các danh sách hành chính. Night mode dùng navy phân lớp, không dùng đen tuyệt đối. Component nên gọi semantic token, không gọi trực tiếp primitive màu rải rác trong markup.

| Vai trò | Light | Night |
|---|---:|---:|
| Primary | `#2157D7` | `#4E83F1` |
| Primary hover | `#1743AE` | `#79A5FF` |
| App background | `#F4F7FC` | `#081326` |
| Canvas | `#FFFFFF` | `#0B1B33` |
| Surface/card | `#FFFFFF` | `#10223D` |
| Surface muted | `#F7F9FD` | `#142A49` |
| Surface hover | `#EEF3FF` | `#19345A` |
| Hero background | `#EEF5FF` | `#102E63` |
| Text primary | `#0B1B3A` | `#F7FAFF` |
| Text secondary | `#42526B` | `#DCE7F8` |
| Text tertiary | `#71819A` | `#A7B9D2` |
| Border default | `#DBE4F0` | `#294263` |
| Focus ring | `rgba(33,87,215,.24)` | `rgba(121,165,255,.34)` |
| Success | `#E8F8F1 / #147D62` | `#0A4238 / #72E0B8` |
| Warning | `#FFF6E5 / #9A6C17` | `#4A310B / #FFD47A` |
| Danger | `#FFF0F2 / #B42335` | `#4A1922 / #FF9EAA` |

Bản JSON đầy đủ được lưu tại [`design-tokens-action-first.json`](./design-tokens-action-first.json).

## CSS variables

Đặt `data-theme="light"` hoặc `data-theme="dark"` ở thẻ `html`. Các component chỉ sử dụng biến `--color-*` và `--status-*`.

```css
:root,
[data-theme="light"] {
  color-scheme: light;
  --color-bg-app: #F4F7FC;
  --color-bg-canvas: #FFFFFF;
  --color-bg-surface: #FFFFFF;
  --color-bg-surface-muted: #F7F9FD;
  --color-bg-surface-hover: #EEF3FF;
  --color-bg-surface-selected: #E7EEFF;
  --color-bg-hero: #EEF5FF;
  --color-bg-sidebar: #0B1B3A;
  --color-text-primary: #0B1B3A;
  --color-text-secondary: #42526B;
  --color-text-tertiary: #71819A;
  --color-text-muted: #98A5B8;
  --color-text-inverse: #FFFFFF;
  --color-border-subtle: #EEF2F8;
  --color-border-default: #DBE4F0;
  --color-border-strong: #BFCDE2;
  --color-border-focus: #2157D7;
  --color-focus-ring: rgba(33, 87, 215, .24);
  --color-action-primary: #2157D7;
  --color-action-primary-hover: #1743AE;
  --color-action-primary-active: #123A91;
  --color-action-secondary: #FFFFFF;
  --color-action-secondary-hover: #EEF3FF;
  --status-success-bg: #E8F8F1;
  --status-success-text: #147D62;
  --status-warning-bg: #FFF6E5;
  --status-warning-text: #9A6C17;
  --status-danger-bg: #FFF0F2;
  --status-danger-text: #B42335;
}

[data-theme="dark"] {
  color-scheme: dark;
  --color-bg-app: #081326;
  --color-bg-canvas: #0B1B33;
  --color-bg-surface: #10223D;
  --color-bg-surface-muted: #142A49;
  --color-bg-surface-hover: #19345A;
  --color-bg-surface-selected: #173B7D;
  --color-bg-hero: #102E63;
  --color-bg-sidebar: #061021;
  --color-text-primary: #F7FAFF;
  --color-text-secondary: #DCE7F8;
  --color-text-tertiary: #A7B9D2;
  --color-text-muted: #7186A5;
  --color-text-inverse: #FFFFFF;
  --color-border-subtle: #172C48;
  --color-border-default: #294263;
  --color-border-strong: #3B5A82;
  --color-border-focus: #79A5FF;
  --color-focus-ring: rgba(121, 165, 255, .34);
  --color-action-primary: #4E83F1;
  --color-action-primary-hover: #79A5FF;
  --color-action-primary-active: #2157D7;
  --color-action-secondary: #172C48;
  --color-action-secondary-hover: #1D3A63;
  --status-success-bg: #0A4238;
  --status-success-text: #72E0B8;
  --status-warning-bg: #4A310B;
  --status-warning-text: #FFD47A;
  --status-danger-bg: #4A1922;
  --status-danger-text: #FF9EAA;
}
```

## Quy tắc triển khai

- `#2157D7` là màu thương hiệu và primary light; không dùng cho toàn bộ text hoặc toàn bộ nền.
- Hero chỉ dùng background tint; CTA chính mới dùng primary solid.
- Bốn action card là vùng ưu tiên cao nhất sau hero: lớp học, lịch hoạt động, tra cứu chứng nhận và khảo sát.
- Các số liệu dashboard chỉ hiển thị khi có API thật; không hard-code số liệu nghiệp vụ chưa được xác thực.
- Badge trạng thái luôn có text, không chỉ dùng màu.
- Focus-visible dùng border primary và ring riêng, không xóa outline trình duyệt mà không thay thế.
- Khi bật night mode, không dùng `#000000` cho nền hoặc `#FFFFFF` cho mọi text; giữ phân cấp bằng surface và text token.
