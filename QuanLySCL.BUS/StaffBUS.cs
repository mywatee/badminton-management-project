using QuanLySCL.DAL;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace QuanLySCL.BUS
{
    public class StaffBUS
    {
        private readonly StaffDAL _staffDal = new StaffDAL();
        private const string CannotDeleteStaffWithHistoryMessage =
            "Nhân viên này đã có lịch sử giao dịch. Không thể xóa, vui lòng sử dụng chức năng Khóa tài khoản!";

        private static bool IsValidPhone(string? phone)
        {
            string p = (phone ?? string.Empty).Trim();
            return Regex.IsMatch(p, @"^0[0-9]{9,10}$");
        }

        public ObservableCollection<Staff> GetAllStaff()
        {
            return _staffDal.GetAllStaff();
        }

        public ObservableCollection<Staff> GetActiveStaff()
        {
            var all = _staffDal.GetAllStaff();
            var active = new ObservableCollection<Staff>();
            foreach (var s in all)
            {
                if (string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    active.Add(s);
            }
            return active;
        }

        public (bool ok, string id, string error) CreateStaffAutoId(string name, string phone, string email, string role, string department, string status, DateTime joinDate)
        {
            if (string.IsNullOrWhiteSpace(name)) return (false, null, "Vui lòng nhập họ tên.");
            if (string.IsNullOrWhiteSpace(phone)) return (false, null, "Vui lòng nhập số điện thoại.");

            if (!IsValidPhone(phone)) return (false, null, "Số điện thoại không hợp lệ (phải bắt đầu bằng 0 và có 10-11 số).");

            string id = _staffDal.GetNextStaffId();
            var staff = new Staff
            {
                Id = id,
                Name = name.Trim(),
                Phone = phone.Trim(),
                // Current DB schema does not include these columns.
                Email = string.Empty,
                Role = (role ?? string.Empty).Trim(),
                Department = "Chung",
                Status = "Active",
                JoinDate = DateTime.Today
            };

            try
            {
                int rows = _staffDal.InsertStaff(staff);
                return rows > 0 ? (true, id, null) : (false, null, "Không thể thêm nhân viên.");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public (bool ok, string error) UpdateStaff(Staff staff)
        {
            if (staff == null) return (false, "Thiếu dữ liệu.");
            if (string.IsNullOrWhiteSpace(staff.Id)) return (false, "Mã nhân viên không hợp lệ.");
            if (string.IsNullOrWhiteSpace(staff.Name)) return (false, "Vui lòng nhập họ tên.");
            if (string.IsNullOrWhiteSpace(staff.Phone)) return (false, "Vui lòng nhập số điện thoại.");

            if (!IsValidPhone(staff.Phone)) return (false, "Số điện thoại không hợp lệ (phải bắt đầu bằng 0 và có 10-11 số).");

            staff.Email = string.Empty;
            staff.Department = "Chung";
            staff.Status = "Active";
            staff.JoinDate = DateTime.Today;

            try
            {
                int rows = _staffDal.UpdateStaff(staff);
                return rows > 0 ? (true, null) : (false, "Không thể cập nhật nhân viên.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool ok, string error) DeleteStaff(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return (false, "Mã nhân viên không hợp lệ.");
            try
            {
                string staffId = id.Trim();

                if (_staffDal.HasLinkedAccount(staffId))
                {
                    return (false, CannotDeleteStaffWithHistoryMessage);
                }

                int rows = _staffDal.DeleteStaff(staffId);
                return rows > 0 ? (true, null) : (false, "Không thể xóa nhân viên.");
            }
            catch (SqlException ex) when (ex.Number == 547) // Foreign key conflict
            {
                return (false, CannotDeleteStaffWithHistoryMessage);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(ex.Message) &&
                    ex.Message.IndexOf("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return (false, CannotDeleteStaffWithHistoryMessage);
                }

                return (false, ex.Message);
            }
        }

        public Staff GetStaffById(string id)
        {
            return _staffDal.GetStaffById(id);
        }
    }
}
