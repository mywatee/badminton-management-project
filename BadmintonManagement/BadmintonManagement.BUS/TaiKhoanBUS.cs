using BadmintonManagement.DAL;
using System;

namespace BadmintonManagement.BUS
{
    public class TaiKhoanBUS
    {
        private TaiKhoanDAL _tkDAL = new TaiKhoanDAL();

        public string DangKyMoi(string hoTen, string sdt, string email, string user, string pass)
        {
            // 1. Kiểm tra trùng
            var check = _tkDAL.CheckExist(user, sdt, email);
            if (check.userCount > 0) return "Tên đăng nhập đã tồn tại!";
            if (check.phoneCount > 0) return "Số điện thoại đã đăng ký!";
            if (check.emailCount > 0) return "Email đã được sử dụng!";

            // 2. Tạo mã KH
            int nextNum = _tkDAL.GetNextMaKHNumber();
            string maKH = "KH" + nextNum.ToString("D3");

            // 3. Mã hóa với Muối (Salt)
            Guid salt = Guid.NewGuid();
            byte[] hash = SecurityHelper.HashPasswordWithSalt(pass, salt);

            // 4. Lưu vào Database
            if (_tkDAL.insertRegistration(maKH, hoTen, sdt, email, user, hash, salt))
            {
                return "SUCCESS:" + maKH; // Trả về mã KH để hiện Dialog thành công
            }
            return "Lỗi hệ thống khi lưu dữ liệu!";
        }
        public (string maKH, string vaiTro) KiemTraDangNhap(string user, string pass)
        {
            // 1. Lấy Salt từ DAL
            object saltObj = _tkDAL.GetSalt(user);
            if (saltObj == null) return (null, null);

            Guid userSalt = (Guid)saltObj;

            // 2. Mã hóa mật khẩu đầu vào (Dùng SecurityHelper ở DAL)
            byte[] hashedInput = BadmintonManagement.DAL.SecurityHelper.HashPasswordWithSalt(pass, userSalt);

            // 3. GỌI HÀM CheckLogin Ở DAL (Hết lỗi đỏ dòng 38)
            return _tkDAL.CheckLogin(user, hashedInput);
        }
    }
}