# TÀI LIỆU HƯỚNG DẪN KIỂM THỬ (TEST PLAN & TEST CASES)
**Dự án:** Quản lý Sân Cầu Lông (QuanLySCL)

Tài liệu này cung cấp các kịch bản kiểm thử (test cases) chi tiết để người dùng/tester có thể kiểm tra toàn diện các chức năng đang có trong hệ thống Quản lý Sân Cầu Lông.

---

## 1. Module Xác thực & Bảo mật (Authentication & Security)

### 1.1 Khởi động và Đăng nhập
- **Mục đích:** Đảm bảo tính năng đăng nhập hoạt động đúng và an toàn.
- **Các bước thực hiện:**
  1. Mở ứng dụng QuanLySCL.
  2. Tại màn hình Đăng nhập (Login), nhập sai thông tin (email không tồn tại hoặc sai mật khẩu) -> Nhấn Đăng nhập. **(Kỳ vọng: Hệ thống báo lỗi "Sai tài khoản hoặc mật khẩu")**
  3. Để trống các trường và nhấn Đăng nhập. **(Kỳ vọng: Hệ thống yêu cầu nhập đầy đủ thông tin)**
  4. Nhập tài khoản hợp lệ (Admin/Nhân viên) và nhấn Đăng nhập. **(Kỳ vọng: Đăng nhập thành công, điều hướng vào Dashboard, phân quyền đúng với Role)**

### 1.2 Đăng ký và OTP (Brevo Integration)
- **Mục đích:** Kiểm tra quy trình đăng ký tài khoản mới qua xác thực OTP email.
- **Các bước thực hiện:**
  1. Tại màn hình Login, nhấn "Đăng ký".
  2. Nhập thông tin không hợp lệ (sai định dạng email/mật khẩu không khớp) -> Nhấn Đăng ký. **(Kỳ vọng: Báo lỗi định dạng)**
  3. Nhập email hợp lệ và thông tin chính xác -> Nhấn Đăng ký. **(Kỳ vọng: Hệ thống gửi OTP về email qua nền tảng Brevo, chuyển sang màn hình nhập OTP)**
  4. Nhập sai mã OTP -> Nhấn Xác nhận. **(Kỳ vọng: Báo lỗi OTP không hợp lệ)**
  5. Nhập đúng mã OTP trong email. **(Kỳ vọng: Đăng ký thành công và tự động đăng nhập hoặc quay lại trang Login)**

### 1.3 Quên mật khẩu
- **Mục đích:** Kiểm tra chức năng lấy lại mật khẩu.
- **Các bước thực hiện:**
  1. Nhấn "Quên mật khẩu" từ màn hình Login.
  2. Nhập email chưa từng đăng ký. **(Kỳ vọng: Cảnh báo email không tồn tại)**
  3. Nhập email đã đăng ký -> Xác nhận. **(Kỳ vọng: Hệ thống gửi mã OTP đặt lại mật khẩu về email)**
  4. Nhập mã OTP và mật khẩu mới. **(Kỳ vọng: Đổi mật khẩu thành công, có thể dùng mật khẩu mới để đăng nhập)**

---

## 2. Module Quản lý Đặt sân (Booking Management)

### 2.1 Tạo mới lịch đặt sân
- **Mục đích:** Đảm bảo nhân viên/khách hàng có thể đặt sân trống.
- **Các bước thực hiện:**
  1. Vào màn hình **Đặt sân (Booking)**.
  2. Chọn ngày, chọn khung giờ (TimeSlot) và chọn Sân (Court) còn trống.
  3. Nhập thông tin Khách hàng (Số điện thoại, Tên) hoặc chọn từ danh sách khách hàng cũ.
  4. Nhấn **Xác nhận đặt sân**. 
  5. **(Kỳ vọng: Trạng thái sân chuyển sang "Đã đặt", tạo mới một bản ghi Booking trong Database, có thông báo thành công và cập nhật UI)**

### 2.2 Huỷ lịch đặt sân
- **Mục đích:** Đảm bảo luồng huỷ lịch hoạt động và bảo mật (không ai thấy Booking của người khác nếu không có quyền).
- **Các bước thực hiện:**
  1. Chọn một Booking đang ở trạng thái "Chờ" hoặc "Đã đặt".
  2. Chọn thao tác **Huỷ đặt sân**.
  3. **(Kỳ vọng: Trạng thái Booking cập nhật thành "Đã huỷ", khung giờ đó của Sân được giải phóng và có thể đặt lại. Dữ liệu thay đổi Real-time trên UI)**

### 2.3 Cập nhật tự động Hoàn thành (Background Job)
- **Mục đích:** Kiểm tra hệ thống tự động cập nhật trạng thái sân.
- **Các bước thực hiện:**
  1. Quan sát một Booking có thời gian kết thúc (End Time) vừa mới trôi qua thời gian hiện tại của máy tính.
  2. **(Kỳ vọng: Hệ thống tự động đổi trạng thái Booking sang "Đã hoàn thành" (Completed) và không cần thao tác thủ công, làm mới UI ngay lập tức)**

---

## 3. Module Dịch vụ & Bán hàng (Services & Point of Sale)

### 3.1 Bán Dịch Vụ (Canteens/Nước uống/Thuê vợt)
- **Mục đích:** Kiểm tra giỏ hàng cho từng phiên đặt sân.
- **Các bước thực hiện:**
  1. Chuyển sang màn hình **Dịch vụ (Services)**.
  2. Chọn một Booking đang diễn ra (hoặc Khách vãng lai).
  3. Thêm các món đồ (Ví dụ: Nước suối, Thuê vợt, Quả cầu...). **(Kỳ vọng: Món đồ hiện lên giỏ hàng (Shopping Cart), tính tổng tiền tạm thời chính xác)**
  4. Tăng/Giảm số lượng hoặc Xóa một món khỏi giỏ. **(Kỳ vọng: Tổng tiền thay đổi tự động tương ứng theo số lượng và giá tiền)**
  5. Nhấn **Thanh toán dịch vụ**. **(Kỳ vọng: Cập nhật CSDL, không gây lỗi trùng lặp dữ liệu, làm sạch giỏ hàng sau khi thành công)**

---

## 4. Bảng điều khiển (Dashboard)

- **Mục đích:** Kiểm tra dữ liệu tổng quan có chính xác không.
- **Các bước thực hiện:**
  1. Mở màn hình **Dashboard**.
  2. So sánh **Tổng số sân đang hoạt động**, **Lượt khách trong ngày**, **Doanh thu tạm tính** với thực tế các Booking vừa tạo hoặc huỷ.
  3. **(Kỳ vọng: Các con số và Biểu đồ phải phản ánh đúng dữ liệu sinh ra trong hệ thống, tự động cập nhật, đồ thị hiển thị trực quan)**

---

## 5. Module Quản trị & Cấu hình (Admin Panel)

### 5.1 Quản lý Sân (Courts Admin)
- Thêm Sân mới: Nhập tên, loại sân -> Lưu. **(Kỳ vọng: Sân mới hiển thị trong màn hình Booking ngay lập tức)**
- Sửa/Xóa sân. **(Kỳ vọng: Hệ thống chặn việc xóa sân đang có Booking)**

### 5.2 Quản trị Khung giờ và Bảng giá (TimeSlots & Pricing)
- Đổi giá tiền theo khung giờ (Giờ cao điểm/Giờ chót).
- Thực hiện đặt sân vào khung giờ vừa đổi giá. **(Kỳ vọng: Tiền sân được tính mới nhất)**

### 5.3 Quản lý Khuyến mãi (Promotions)
- Tạo một mã Voucher giảm giá 10% hoặc thẻ Thành viên.
- Áp dụng vào phần Thanh toán Booking. **(Kỳ vọng: Giảm trừ đúng 10% vào tổng hoá đơn)**

### 5.4 Quản lý Nhân viên (Staff)
- Thêm/Sửa/Xóa Nhân viên.
- Thay đổi quyền (Role) của một tài khoản. **(Kỳ vọng: Phân quyền theo Role hoạt động đúng khi Account đăng nhập lại)**

---

## 6. Module Quản lý Khách hàng & Báo cáo (Customers & Reports)

### 6.1 Khách hàng (Customers)
1. Truy cập tab **Khách hàng**.
2. Tìm kiếm khách hàng theo tên hoặc số điện thoại. **(Kỳ vọng: Lọc kết quả đúng trong danh sách lưới)**
3. Xem lịch sử đặt sân của một khách hàng. **(Kỳ vọng: Thống kê đầy đủ, không hiển thị sai lệch)**

### 6.2 Báo cáo & Thống kê (Reports)
1. Truy cập tab **Báo cáo**.
2. Chọn khoảng thời gian (Từ ngày A -> Đến ngày B) và xuất báo cáo.
3. **(Kỳ vọng: Tính toán chuẩn xác Doanh thu, phân bổ giữa Tiền sân và Tiền Dịch vụ rõ ràng)**
