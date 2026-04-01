using QuanLySCL.DAL;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QuanLySCL.BUS
{
    public class TaiKhoanBUS
    {
        private readonly TaiKhoanDAL _dal = new TaiKhoanDAL();
        private readonly EmailService _emailService = new EmailService();

        public (string maKH, string vaiTro) DangNhap(string user, string pass)
        {
            return _dal.KiemTraDangNhap(user, pass);
        }

        public (bool ok, string msg) DangKy(string ten, string sdt, string email, string user, string pass)
        {
            return _dal.DangKyTaiKhoan(ten, sdt, email, user, pass);
        }

        public (bool ok, string msg) KiemTraHopLeDangKy(string user, string phone, string email)
        {
            var res = _dal.KiemTraTrung(user, phone, email);
            if (res.user > 0) return (false, "Tên đăng nhập đã tồn tại!");
            if (res.phone > 0) return (false, "Số điện thoại đã được đăng ký!");
            if (res.email > 0) return (false, "Email đã được sử dụng!");
            return (true, "Hợp lệ");
        }

        public async Task<(bool success, string errorMsg)> GuiOTPDangKy(string email, string otp)
        {
            return await _emailService.SendOTPAsync(email, otp);
        }

        public async Task<(bool success, string errorMsg)> GuiOTPQuenMK(string email, string otp)
        {
            return await _emailService.SendOTPAsync(email, otp);
        }

        public string LayMaKH(string info, bool laEmail)
        {
            return _dal.LayMaKHTheoThongTin(info, laEmail);
        }

        public bool DoiMatKhau(string maKH, string passMoi)
        {
            return _dal.CapNhatMatKhau(maKH, passMoi);
        }

        public ObservableCollection<Account> GetAllAccounts()
        {
            return _dal.GetAllAccounts();
        }

        public bool SetAccountStatus(string username, bool isActive)
        {
            return _dal.SetAccountStatus(username, isActive);
        }

        public Account GetAccountByStaffId(string staffId)
        {
            return _dal.GetAccountByStaffId(staffId);
        }

        public (bool ok, string msg) TaoTaiKhoanNhanVien(string staffId, string username, string password, string role, bool isActive)
        {
            var res = _dal.TaoTaiKhoanNhanVien(staffId, username, password, role, isActive);
            return (res.ok, res.message);
        }

        public (bool ok, string msg) ResetMatKhauTheoTenDangNhap(string username, string newPassword)
        {
            var res = _dal.ResetMatKhauTheoTenDangNhap(username, newPassword);
            return (res.ok, res.message);
        }

        public (bool ok, string msg) CapNhatQuyenTheoTenDangNhap(string username, string role)
        {
            var res = _dal.CapNhatQuyenTheoTenDangNhap(username, role);
            return (res.ok, res.message);
        }

        public (bool ok, string msg) DoiTenDangNhap(string oldUsername, string newUsername)
        {
            var res = _dal.DoiTenDangNhap(oldUsername, newUsername);
            return (res.ok, res.message);
        }

        public (bool ok, string msg) XoaTaiKhoanTheoTenDangNhap(string username)
        {
            var res = _dal.XoaTaiKhoanTheoTenDangNhap(username);
            return (res.ok, res.message);
        }
    }
}
