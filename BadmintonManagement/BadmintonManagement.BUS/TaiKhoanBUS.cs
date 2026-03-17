using BadmintonManagement.DAL;
using System;

namespace BadmintonManagement.BUS
{
    public class TaiKhoanBUS
    {
        private TaiKhoanDAL _tkDAL = new TaiKhoanDAL();

        public string DangKyMoi(string hoTen, string sdt, string email, string user, string pass)
        {
            // Sửa lỗi biến em ail -> email
            var check = _tkDAL.CheckExist(user, sdt, email);

            if (check.userCount > 0) return "Tên đăng nhập đã tồn tại!";
            if (check.phoneCount > 0) return "Số điện thoại đã đăng ký!";
            if (check.emailCount > 0) return "Email đã được sử dụng!";

            int nextNum = _tkDAL.GetNextMaKHNumber();
            string maKH = "KH" + nextNum.ToString("D3"); // Xóa khoảng trắng trong format

            Guid salt = Guid.NewGuid();
            byte[] hash = SecurityHelper.HashPasswordWithSalt(pass, salt);

            if (_tkDAL.insertRegistration(maKH, hoTen, sdt, email, user, hash, salt))
            {
                return "SUCCESS:" + maKH;
            }
            return "Lỗi hệ thống khi lưu dữ liệu!";
        }

        public (string maKH, string vaiTro) KiemTraDangNhap(string user, string pass)
        {
            object saltObj = _tkDAL.GetSalt(user);
            if (saltObj == null) return (null, null);

            if (saltObj is Guid userSalt)
            {
                // Sửa lỗi has hedInput -> hashedInput
                byte[] hashedInput = SecurityHelper.HashPasswordWithSalt(pass, userSalt);
                return _tkDAL.CheckLogin(user, hashedInput);
            }

            return (null, null);
        }
    }
}