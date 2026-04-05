# CHƯƠNG 5: DEMO VÀ XÂY DỰNG CHƯƠNG TRÌNH

## 5.1. Mục tiêu chương

Chương này trình bày quy trình **xây dựng – cài đặt – cấu hình – chạy thử** và **demo các chức năng chính** của phần mềm **Quản Lý Sân Cầu Lông (QuanLySCL)**. Nội dung tập trung vào:

- Thiết lập môi trường và công nghệ sử dụng.
- Khởi tạo cơ sở dữ liệu (CSDL) và dữ liệu nền phục vụ vận hành.
- Biên dịch (build) và chạy chương trình.
- Demo theo kịch bản nghiệp vụ (Admin / Nhân viên / Khách hàng).
- Gợi ý kiểm thử tự động (E2E) và xuất báo cáo.

### 5.1.1. Danh sách hình minh họa (demo chức năng chính)

Khi viết báo cáo Word, mỗi hình nên có caption theo mẫu: `Hình 5.x. <Tên hình>`. Danh sách hình đề xuất:

1. **Hình 5.1**: Màn hình đăng nhập (`LoginWindow`)
2. **Hình 5.2**: Giao diện chính theo vai trò (Admin / Nhân viên / Khách hàng) (`MainWindow`)
3. **Hình 5.3**: Quản lý sân / loại sân (Admin – Courts)
4. **Hình 5.4**: Tạo nhanh ca giờ + giá (Admin – Quick Generate Time Slots & Prices)
5. **Hình 5.5**: Danh sách ca giờ sau khi tạo (Admin – Time Slots)
6. **Hình 5.6**: Quản lý bảng giá theo ca (Admin – Pricing)
7. **Hình 5.7**: Tạo phiếu đặt sân (Booking – Create Booking)
8. **Hình 5.8**: Chi tiết phiếu đặt / trạng thái (Booking Detail)
9. **Hình 5.9**: Check-out + thanh toán + hóa đơn (CheckOut / Invoice)
10. **Hình 5.10**: Bán dịch vụ (POS – Services/Cart)
11. **Hình 5.11**: Áp dụng khuyến mãi (Promotion)
12. **Hình 5.12**: Báo cáo – thống kê (Dashboard/Reports, biểu đồ LiveCharts)

Ghi chú: Trong Word, phần "Danh sách hình" thường đặt ở đầu báo cáo (tùy quy định). Trong Chương 5, chỉ cần đặt caption ngay dưới hình.

### 5.1.2. Kịch bản demo tổng quát (tóm tắt)

- Chuẩn bị DB `QLSCL`, seed sân/ca giờ/bảng giá/dịch vụ.
- Demo theo 3 vai trò: Admin (cấu hình), Nhân viên (vận hành), Khách hàng (đặt sân).
- Thực hiện luồng nghiệp vụ: **Đặt sân → Check-in → Thêm dịch vụ (nếu có) → Check-out → Hóa đơn → Báo cáo**.



---

## 5.2. Tổng quan kiến trúc và các module trong project

Project được tổ chức theo mô hình nhiều tầng, tách biệt rõ trách nhiệm:

- `QuanLySCL.Models`: các lớp mô hình dữ liệu (Booking, Court, Customer, TimeSlot, PriceEntry, Invoice, Service, Promotion, …).
- `QuanLySCL.DAL`: tầng truy cập dữ liệu (SQL Server) bằng `Microsoft.Data.SqlClient` (CRUD + truy vấn nghiệp vụ).
- `QuanLySCL.BUS`: tầng nghiệp vụ (gọi DAL, kiểm tra/điều phối quy trình như đặt sân, check-in/out, dịch vụ, báo cáo, tài khoản).
- `QuanLySCL.GUI`: ứng dụng WPF (MaterialDesignThemes, LiveCharts, WebView2), giao diện theo vai trò.
- `QuanLySCL.E2E`: kiểm thử end-to-end bằng Playwright + NUnit, có thể xuất báo cáo Excel.

Luồng xử lý tổng quát:

`GUI (View/ViewModel) → BUS → DAL → SQL Server (QLSCL)`

---

## 5.3. Môi trường cài đặt và yêu cầu hệ thống

### 5.3.1. Phần mềm cần cài đặt

- Windows 10/11
- **.NET SDK 10** (project đang target `net10.0` và `net10.0-windows`)  
  - GUI WPF: `QuanLySCL.GUI` target `net10.0-windows`
- Microsoft SQL Server (khuyến nghị **SQL Server Express** hoặc bản Developer)
- (Tuỳ chọn) Visual Studio (khuyến nghị khi làm WPF): workload “.NET Desktop Development”

### 5.3.2. Cấu hình kết nối CSDL

Kết nối DB được cấu hình tại `QuanLySCL.DAL/BaseDAL.cs` thông qua:

- Biến môi trường `QLSCL_CONNECTION_STRING` (ưu tiên)
- Nếu không có env var thì dùng mặc định:
  - `Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True`

Ví dụ cấu hình env var (PowerShell):

```powershell
$env:QLSCL_CONNECTION_STRING="Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True"
```

---

## 5.4. Xây dựng cơ sở dữ liệu và dữ liệu nền

### 5.4.1. Tạo schema CSDL

Script khởi tạo schema:

- `sqlfinal.sql`: tạo database `QLSCL` và các bảng (ví dụ: `CA_GIO`, `BANG_GIA`, `LOAI_SAN`, `SAN`, `KHACH_HANG`, `TAI_KHOAN`, `DAT_SAN`, `CT_DAT_SAN`, `DICH_VU`, `HOA_DON`, …).

Thực hiện:

1. Mở SQL Server Management Studio (SSMS)
2. Chạy `sqlfinal.sql`

### 5.4.2. Seed ca giờ (CA_GIO) và bảng giá (BANG_GIA)

Hệ thống cần có dữ liệu **ca giờ** và **bảng giá** để tính tiền/đặt sân.

Có 2 cách:

**Cách A – Chạy script SQL (khuyến nghị khi triển khai nhanh):**

- `scripts/seed_timeslots_30m_and_prices.sql`  
  - Tạo ca theo bước 30 phút (mặc định 06:00–22:00)
  - Có thể cấu hình khung “giờ vàng” (mặc định 18:00–21:00)
  - Upsert `CA_GIO` và suy luận/seed `BANG_GIA` dựa trên giá có sẵn
- Lưu ý: script sẽ **bỏ qua** rule nếu không suy luận được giá nền.

**Cách B – Dùng chức năng tạo nhanh trong GUI:**

- Cửa sổ `QuickGenerateTimeSlotsWindow` cho phép:
  - Nhập giờ mở/đóng cửa, bước phút, khung giờ vàng
  - Nhập giá theo loại đặt (Lẻ / Cố định) và theo giờ thường/giờ vàng
  - (Tuỳ chọn) suy luận giá từ dữ liệu hiện có
  - Upsert xuống DB qua `AdminBUS.UpsertTimeSlotsAndPrices(...)`

### 5.4.3. Dữ liệu dịch vụ (DICH_VU)

Có script cập nhật danh sách dịch vụ (đồng bộ giá, xoá “thuê giày”, chuẩn hoá “ống cầu”):

- `scripts/update_services_prices.sql`

Ngoài ra, ở runtime, `QuanLySCL.BUS/ServiceBUS.cs` có cơ chế “best-effort” đảm bảo tồn tại một số sản phẩm ống cầu mẫu (không bắt buộc).

### 5.4.4. Tạo tài khoản Admin ban đầu

Do `sqlfinal.sql` chủ yếu tạo schema, để có **Admin** đăng nhập lần đầu có thể làm theo hướng an toàn nhất:

1. Dùng màn hình **Đăng ký** trong app để tạo một tài khoản (đã có hash/salt hợp lệ).
2. Chạy lệnh SQL để nâng quyền:

```sql
UPDATE TAI_KHOAN
SET VaiTro = N'Admin'
WHERE TenDangNhap = 'ten_dang_nhap_can_cap_quyen';
```

---

## 5.5. Biên dịch (build) và chạy chương trình

### 5.5.1. Build bằng Visual Studio (khuyến nghị)

1. Mở project/solution trong Visual Studio
2. Chọn Startup Project: `QuanLySCL.GUI`
3. Build cấu hình `Debug` hoặc `Release`
4. Run để mở ứng dụng

### 5.5.2. Build bằng .NET CLI

Tại thư mục repo:

```powershell
dotnet restore .\QuanLySCL.GUI\QuanLySCL.GUI.csproj
dotnet build   .\QuanLySCL.GUI\QuanLySCL.GUI.csproj -c Release
dotnet run     --project .\QuanLySCL.GUI\QuanLySCL.GUI.csproj
```

Lưu ý:

- Khi chạy lần đầu, cần đảm bảo kết nối SQL Server và DB `QLSCL` đã sẵn sàng.
- Ứng dụng khởi động tại `QuanLySCL.GUI/App.xaml.cs`, hiển thị `LoginWindow` trước, sau đó mở `MainWindow`.

---

## 5.6. Demo ch??ng tr?nh v? h??ng d?n s? d?ng (chi ti?t)

M?c 5.6 tr?nh b?y demo theo **lu?ng nghi?p v? th?c t?** c?a m?t c? s? s?n c?u l?ng. M?i ch?c n?ng ??u c?: **m?c ??ch ? ti?n ?i?u ki?n ? thao t?c ? d? li?u minh h?a ? k?t qu? mong ??i ? l?i th??ng g?p ? h?nh ch?p ?? xu?t**.

### 5.6.1. ??ng nh?p, ph?n quy?n v? ?i?u h??ng theo vai tr?

**M?c ??ch**

- X?c th?c ng??i d?ng.
- Hi?n th? ??ng menu/ch?c n?ng theo vai tr? (Admin/Nh?n vi?n/Kh?ch h?ng).
- ?i?u h??ng m?c ??nh ??ng theo vai tr?.

**Ti?n ?i?u ki?n**

- C? ?t nh?t 01 t?i kho?n h?p l? trong `TAI_KHOAN`.
- ?? c?u h?nh k?t n?i DB `QLSCL`.

**Thao t?c demo**

1. M? ?ng d?ng ? m?n h?nh `LoginWindow`.
2. Nh?p **T?n ??ng nh?p** v? **M?t kh?u**.
3. Nh?n **??ng nh?p**.
4. Quan s?t `MainWindow`:
   - T?n ng??i d?ng v? vai tr? hi?n th? ??ng.
   - Menu/nh?m ch?c n?ng hi?n th? ??ng quy?n.
   - Trang m?c ??nh theo vai tr?:
     - Admin ? trang qu?n tr?.
     - Kh?ch h?ng ? trang ??t s?n (Booking).
     - Nh?n vi?n ? Dashboard.

**D? li?u minh h?a**

- T?i kho?n Admin: d?ng ?? demo ph?n qu?n tr? (Courts/TimeSlots/Pricing/Promotions/Accounts).
- T?i kho?n Nh?n vi?n: d?ng ?? demo v?n h?nh (booking, check-in/out, POS).
- T?i kho?n Kh?ch h?ng: d?ng ?? demo ??t s?n ph?a kh?ch.

**K?t qu? mong ??i**

- ??ng nh?p th?nh c?ng ? v?o `MainWindow`.
- Ng??i d?ng **kh?ng c? quy?n** s?:
  - kh?ng th?y menu t??ng ?ng, ho?c
  - b? c?nh b?o khi c? truy c?p (n?u c?) v? b? ??a v? trang ph? h?p.

**L?i th??ng g?p & c?ch x? l?**

- Kh?ng ??ng nh?p ???c: ki?m tra `QLSCL_CONNECTION_STRING`, DB `QLSCL` c? t?n t?i, SQL Server ?ang ch?y.
- Sai m?t kh?u: th? ch?c n?ng qu?n m?t kh?u (n?u tri?n khai) ho?c t?o l?i t?i kho?n.

**H?nh minh h?a ?? xu?t**

- H?nh 5.1: M?n h?nh ??ng nh?p.
- H?nh 5.2: Giao di?n ch?nh theo vai tr?.

---

### 5.6.2. Qu?n tr? lo?i s?n v? s?n (Admin)

**M?c ??ch**

- T?o d? li?u n?n v? **lo?i s?n** v? **s?n** ?? ph?c v?: ??t s?n, t?nh ti?n theo lo?i s?n, b?o c?o doanh thu theo s?n.

**Ti?n ?i?u ki?n**

- ??ng nh?p v?i vai tr? **Admin**.
- CSDL c? b?ng `LOAI_SAN`, `SAN`.

**Thao t?c demo (g?i ? k?ch b?n)**

1. V?o menu Admin ? **Courts**.
2. Th?m m?i **Lo?i s?n**:
   - V? d?: ?S?n ti?u chu?n?, ?S?n VIP?.
3. Th?m m?i **S?n**:
   - Ch?n lo?i s?n t??ng ?ng.
   - Tr?ng th?i ban ??u: ?Available/Tr?ng? (t?y h? th?ng).
4. S?a th?ng tin 01 s?n (??i t?n/??i lo?i/??i tr?ng th?i) ?? ch?ng minh CRUD.

**K?t qu? mong ??i**

- Lo?i s?n v? s?n hi?n th? trong danh s?ch.
- D? li?u ???c l?u DB v? d?ng ???c ? m?n Booking.

**L?i th??ng g?p & c?ch x? l?**

- Kh?ng l?u ???c: ki?m tra kh?a ch?nh/unique trong DB, ki?m tra format m? s?n (n?u c? quy ??c).

**H?nh minh h?a ?? xu?t**

- H?nh 5.3: Qu?n l? s?n/lo?i s?n.

---

### 5.6.3. T?o ca gi? (CA_GIO) v? b?ng gi? (BANG_GIA) (Admin)

**M?c ??ch**

- Chu?n h?a **khung gi?** ?? ??t s?n theo ca.
- C?u h?nh **b?ng gi?** theo `(Lo?i s?n, Ca, Lo?i ??t)` ?? t?nh ti?n t? ??ng.

**Ti?n ?i?u ki?n**

- ?? c? ?t nh?t 01 lo?i s?n trong `LOAI_SAN`.
- C? d? li?u gi? (n?u d?ng ch? ?? ?suy lu?n?), ho?c nh?p gi? th? c?ng.

**C?ch A ? Demo t?o nhanh trong GUI (khuy?n ngh? ?? ch?p h?nh)**

1. V?o Admin ? **Time Slots** (ho?c m? c?a s? t?o nhanh).
2. M? `QuickGenerateTimeSlotsWindow`.
3. Nh?p c?u h?nh ca:
   - Open: `06:00`
   - Close: `22:00`
   - Step: `30` (ph?t)
   - PeakStart/PeakEnd (tu? ch?n): `18:00` ? `21:00`
4. Nh?p gi? m?u (c? th? ?i?u ch?nh theo ?? t?i):
   - L? ? gi? th??ng: 80.000
   - L? ? gi? v?ng: 100.000
   - C? ??nh ? gi? th??ng: 70.000
   - C? ??nh ? gi? v?ng: 90.000
5. Ch?n **Overwrite** n?u mu?n c?p nh?t l?i d? li?u ?? c?.
6. Nh?n **Generate**.
7. Ki?m tra:
   - Danh s?ch ca gi? (Time Slots).
   - Danh s?ch gi? (Pricing).

**K?t qu? mong ??i**

- T?o c?c ca 30 ph?t t? 06:00 ??n 22:00.
- Ca trong [18:00, 21:00) ???c ??nh d?u gi? v?ng (n?u c?u h?nh).
- B?ng gi? c? rule cho m?i lo?i s?n v? m?i ca, v?i 2 lo?i ??t: **L?** v? **C? ??nh**.

**C?ch B ? Demo b?ng script SQL (ph? h?p tri?n khai nhanh)**

- Ch?y `scripts/seed_timeslots_30m_and_prices.sql`.
- Quan s?t th?ng b?o `inserted/updated/skipped` cho `CA_GIO` v? `BANG_GIA`.

**L?i th??ng g?p & c?ch x? l?**

- Kh?ng suy lu?n ???c gi? (skipped nhi?u): c?n t?o tr??c m?t s? d?ng `BANG_GIA` l?m ?gi? n?n? ho?c nh?p gi? th? c?ng trong GUI.
- Close ? Open: nh?p l?i th?i gian.
- Step kh?ng h?p l?: n?n d?ng 30.

**H?nh minh h?a ?? xu?t**

- H?nh 5.4: C?a s? t?o nhanh ca gi? + gi?.
- H?nh 5.5: Danh s?ch ca gi? sau t?o.
- H?nh 5.6: Qu?n l? b?ng gi? theo ca.

---

### 5.6.4. T?o phi?u ??t s?n (Booking) ? lu?ng ??t s?n c? b?n

**M?c ??ch**

- Demo nghi?p v? ??t s?n theo ng?y/ca.
- Ki?m tra ch?ng tr?ng l?ch v? t?nh gi? t? ??ng.

**Ti?n ?i?u ki?n**

- C? d? li?u: `SAN`, `CA_GIO`, `BANG_GIA`.
- T?i kho?n kh?ch h?ng ho?c nh?n vi?n c? quy?n t?o booking.

**Thao t?c demo (g?i ?)**

1. V?o menu **Booking**.
2. Ch?n **Ng?y s? d?ng** (v? d?: h?m nay).
3. Ch?n **S?n**.
4. Ch?n **1 ca** (ho?c nhi?u ca li?n ti?p n?u ???c h? tr?).
5. Ch?n **Lo?i ??t**:
   - `L?` (??t theo l?n), ho?c
   - `C? ??nh` (??t theo g?i/c? ??nh).
6. (Tu? ch?n) ch?n th?m d?ch v? k?m theo booking n?u UI h? tr? ngay l?c t?o.
7. Nh?n **T?o/X?c nh?n**.

**D? li?u minh h?a**

- Ng?y: 05/04/2026
- Ca: 18:00?18:30 (gi? v?ng)
- Lo?i ??t: L?

**K?t qu? mong ??i**

- H? th?ng ki?m tra ca tr?ng.
- N?u h?p l?: t?o phi?u `DAT_SAN` v? chi ti?t `CT_DAT_SAN`.
- T?ng ti?n s?n l?y t? `BANG_GIA` theo lo?i s?n + ca + lo?i ??t.

**Demo t?nh hu?ng tr?ng l?ch (b?t bu?c ?? th? hi?n t?nh ??ng ??n)**

1. T?o 01 booking ? ca X.
2. T?o ti?p booking kh?c c?ng s?n, c?ng ng?y, c?ng ca X.
3. H? th?ng ph?i b?o ca b?n v? kh?ng cho ??t tr?ng.

**H?nh minh h?a ?? xu?t**

- H?nh 5.7: M?n t?o booking.
- H?nh 5.8: M?n chi ti?t booking / tr?ng th?i.

---

### 5.6.5. Check-in ? Check-out ? Thanh to?n ? Xu?t h?a ??n

**M?c ??ch**

- Demo quy tr?nh v?n h?nh th?c t?: kh?ch ??n ? check-in ? s? d?ng s?n ? check-out ? thanh to?n ? l?u h?a ??n.

**Ti?n ?i?u ki?n**

- C? ?t nh?t 01 booking h?p l? ? tr?ng th?i ch?.
- C? gi? s?n; (tu? ch?n) c? d?ch v? ?? c?ng th?m ti?n.

**Thao t?c demo**

**A. Check-in**

1. M? chi ti?t phi?u ??t.
2. Nh?n **Check-in**.
3. Quan s?t:
   - Tr?ng th?i phi?u ??t ??i sang ?Checked-in/Nh?n s?n?.
   - Tr?ng th?i s?n ??i sang ?In-use/?ang s? d?ng? (n?u c? theo thi?t k?).

**B. Th?m d?ch v? (n?u demo d?ch v? g?n v?i booking)**

1. ? m?n chi ti?t booking, ch?n d?ch v? (n??c, ?ng c?u, ?).
2. Nh?p s? l??ng.
3. X?c nh?n th?m.

**C. Check-out + thanh to?n**

1. Nh?n **Check-out**.
2. Ki?m tra:
   - Ti?n s?n.
   - Ti?n d?ch v?.
   - S? ti?n gi?m (n?u c? khuy?n m?i).
3. Ch?n ph??ng th?c thanh to?n (ti?n m?t/chuy?n kho?n/VietQR n?u c?).
4. X?c nh?n ? h? th?ng t?o `HOA_DON`.

**K?t qu? mong ??i**

- H?a ??n ???c t?o v? l?u DB.
- Booking chuy?n tr?ng th?i ho?n t?t.
- S?n tr? v? tr?ng th?i r?nh.

**L?i th??ng g?p & c?ch x? l?**

- Kh?ng t?o ???c h?a ??n: ki?m tra r?ng bu?c kh?a ngo?i v? d? li?u d?ch v?.

**H?nh minh h?a ?? xu?t**

- H?nh 5.9: M?n check-out v? th?ng tin h?a ??n.

---

### 5.6.6. B?n d?ch v? (POS) ? t?o h?a ??n d?ch v? ??c l?p

**M?c ??ch**

- Demo b?n d?ch v? kh?ng ph? thu?c booking (m? h?nh POS).

**Ti?n ?i?u ki?n**

- C? d? li?u trong `DICH_VU`.
- (Tu? ch?n) ?? ch?y `scripts/update_services_prices.sql` ?? chu?n ho? danh m?c.

**Thao t?c demo**

1. V?o menu **Services**.
2. Ch?n 2?3 s?n ph?m v? th?m v?o gi?.
3. Ch?nh s? l??ng.
4. (Tu? ch?n) nh?p **m? khuy?n m?i**.
5. Nh?n **Checkout** ? ch?n ph??ng th?c thanh to?n ? x?c nh?n.

**K?t qu? mong ??i**

- T?o h?a ??n d?ch v? (ho?c b?n ghi b?n h?ng theo thi?t k?).
- T?n kho gi?m t??ng ?ng (n?u h? th?ng qu?n l? t?n kho).

**H?nh minh h?a ?? xu?t**

- H?nh 5.10: M?n POS/gi? h?ng/checkout.

---

### 5.6.7. Qu?n l? v? ?p d?ng khuy?n m?i

**M?c ??ch**

- Demo t?o m? khuy?n m?i v? ?p d?ng gi?m gi? khi thanh to?n.

**Ti?n ?i?u ki?n**

- Admin c? quy?n t?o khuy?n m?i.

**Thao t?c demo**

1. Admin v?o **Promotions** ? t?o m? (v? d?: `KM10`).
2. C?u h?nh ?i?u ki?n (n?u c?): s? ti?n t?i thi?u, ng?y hi?u l?c.
3. Khi thanh to?n (booking ho?c POS): nh?p m? `KM10` ? ?p d?ng.
4. Quan s?t s? ti?n gi?m v? t?ng ti?n sau gi?m.

**K?t qu? mong ??i**

- M? h?p l? ? h? th?ng t?nh gi?m ??ng.
- M? h?t h?n/kh?ng ??t ?i?u ki?n ? th?ng b?o r? r?ng.

**H?nh minh h?a ?? xu?t**

- H?nh 5.11: Qu?n l?/?p d?ng khuy?n m?i.

---

### 5.6.8. B?o c?o ? th?ng k? (Dashboard/Reports)

**M?c ??ch**

- Demo c?c b?o c?o qu?n tr?: doanh thu, top kh?ch h?ng, xu h??ng theo th?i gian.

**Ti?n ?i?u ki?n**

- C? d? li?u ph?t sinh: ?t nh?t 1?2 h?a ??n (t? check-out ho?c POS).

**Thao t?c demo**

1. V?o **Dashboard** ho?c **Reports**.
2. Ch?n kho?ng th?i gian (n?u c? l?a ch?n).
3. Quan s?t:
   - T?ng doanh thu ng?y/th?ng.
   - Top kh?ch h?ng.
   - Bi?u ?? xu h??ng (LiveCharts).

**K?t qu? mong ??i**

- S? li?u kh?p v?i d? li?u h?a ??n v? phi?u ??t.
- Bi?u ?? hi?n th? ??ng xu h??ng.

**H?nh minh h?a ?? xu?t**

- H?nh 5.12: M?n b?o c?o ? th?ng k?.


## 5.7. Kiểm thử tự động (E2E) và xuất báo cáo Excel (tuỳ chọn)

Module `QuanLySCL.E2E` sử dụng:

- NUnit (`Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`)
- Playwright (`Microsoft.Playwright.NUnit`)
- ClosedXML để xuất báo cáo Excel

Cấu hình qua biến môi trường (xem `QuanLySCL.E2E/Config/E2ESettings.cs`):

- `E2E_BASE_URL`: URL của hệ thống để Playwright mở trình duyệt (nếu có môi trường web/test host)
- `E2E_WORD_SPEC`: đường dẫn file Word `chucnang.docx` để trích 5 chức năng chính
- `E2E_EXCEL_TEMPLATE`: template Excel (ví dụ `TemplateTest.xlsx`)
- `E2E_OUTPUT_DIR`: thư mục xuất kết quả (mặc định `TestResults`)
- `E2E_REPEAT`: số lần lặp mỗi test case

Chạy test:

```powershell
dotnet test .\QuanLySCL.E2E\QuanLySCL.E2E.csproj -c Release
```

Kết quả Excel được ghi bởi `QuanLySCL.E2E/Reporting/ExcelReportWriter.cs`.

---

## 5.8. Tổng kết

Qua demo, hệ thống thể hiện đầy đủ các nhóm chức năng cốt lõi:

- Quản trị dữ liệu nền: sân, ca giờ, bảng giá, khuyến mãi, tài khoản.
- Đặt sân và kiểm soát lịch bận theo ca.
- Check-in/check-out, tính tiền và hoá đơn.
- Quản lý/bán dịch vụ (POS), áp dụng khuyến mãi.
- Báo cáo – thống kê doanh thu, top khách hàng và biểu đồ.

Đây là nền tảng để triển khai thực tế cho một cơ sở kinh doanh sân cầu lông với quy trình vận hành rõ ràng, dễ mở rộng.

