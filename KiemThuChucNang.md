# Kế hoạch kiểm thử các chức năng - Quản Lý Sân Cầu Lông

Đây là tài liệu phác thảo các kịch bản kiểm thử (test case) cho các chức năng chính của hệ thống.

---

## 1. Chức năng Đăng nhập (Login)

-   **TC1.1 (Happy Path):** Đăng nhập thành công với tài khoản và mật khẩu đúng của vai trò Admin.
    -   **Mong muốn:** Chuyển hướng đến màn hình Admin Panel.
-   **TC1.2 (Happy Path):** Đăng nhập thành công với tài khoản và mật khẩu đúng của vai trò Nhân viên.
    -   **Mong muốn:** Chuyển hướng đến màn hình chính cho nhân viên.
-   **TC1.3 (Negative):** Đăng nhập với sai mật khẩu.
    -   **Mong muốn:** Hiển thị thông báo "Sai tên đăng nhập hoặc mật khẩu".
-   **TC1.4 (Negative):** Đăng nhập với sai tên đăng nhập.
    -   **Mong muốn:** Hiển thị thông báo "Sai tên đăng nhập hoặc mật khẩu".
-   **TC1.5 (Negative):** Đăng nhập với tài khoản đã bị khóa.
    -   **Mong muốn:** Hiển thị thông báo "Tài khoản đã bị khóa".
-   **TC1.6 (Edge Case):** Để trống tên đăng nhập hoặc mật khẩu.
    -   **Mong muốn:** Nút "Đăng nhập" bị vô hiệu hóa hoặc hiển thị thông báo yêu cầu nhập đủ thông tin.

---

## 2. Chức năng Đặt sân (Booking)

-   **TC2.1 (Happy Path):** Nhân viên tạo một phiếu đặt sân mới cho khách vãng lai.
    -   **Mong muốn:** Phiếu đặt được tạo thành công với trạng thái "Chờ thanh toán". Lịch sân được cập nhật.
-   **TC2.2 (Happy Path):** Nhân viên tạo phiếu đặt sân cho khách hàng thành viên.
    -   **Mong muốn:** Phiếu đặt được tạo, thông tin khách hàng được liên kết.
-   **TC2.3 (Negative):** Cố gắng đặt một sân đã có người khác đặt trong cùng một khung giờ.
    -   **Mong muốn:** Hệ thống hiển thị thông báo "Sân đã được đặt" và không cho phép tạo.
-   **TC2.4 (Happy Path):** Thêm các dịch vụ (nước uống, cầu...) vào một phiếu đặt.
    -   **Mong muốn:** Tổng tiền của phiếu đặt được cập nhật chính xác.
-   **TC2.5 (Happy Path):** Hủy một phiếu đặt.
    -   **Mong muốn:** Phiếu đặt chuyển sang trạng thái "Đã hủy". Lịch sân được giải phóng.

---

## 3. Chức năng Thanh toán (Checkout)

-   **TC3.1 (Happy Path):** Thanh toán một phiếu đặt không có khuyến mãi.
    -   **Mong muốn:** Hóa đơn được tạo, trạng thái phiếu đặt chuyển thành "Đã hoàn thành".
-   **TC3.2 (Happy Path):** Thanh toán một phiếu đặt có áp dụng mã khuyến mãi hợp lệ.
    -   **Mong muốn:** Hóa đơn được tạo với số tiền được giảm.
-   **TC3.3 (Negative):** Áp dụng một mã khuyến mãi hết hạn hoặc không hợp lệ.
    -   **Mong muốn:** Hiển thị thông báo "Mã khuyến mãi không hợp lệ".
-   **TC3.4 (Happy Path):** In hóa đơn sau khi thanh toán.
    -   **Mong muốn:** Mở cửa sổ xem trước bản in của hóa đơn với đầy đủ thông tin.

---

## 4. Chức năng Quản lý (Admin Panel)

-   **TC4.1 (Authorization):** Tài khoản nhân viên cố gắng truy cập vào Admin Panel.
    -   **Mong muốn:** Truy cập bị từ chối hoặc không thấy mục Admin Panel.
-   **TC4.2 (Happy Path):** Admin thêm, sửa, xóa một nhân viên.
    -   **Mong muốn:** Các thay đổi được cập nhật chính xác trong danh sách nhân viên.
-   **TC4.3 (Happy Path):** Admin thêm, sửa, xóa một sân.
    -   **Mong muốn:** Các thay đổi được cập nhật chính xác.
-   **TC4.4 (Happy Path):** Admin xem báo cáo doanh thu theo ngày/tháng/năm.
    -   **Mong muốn:** Báo cáo hiển thị dữ liệu chính xác.
